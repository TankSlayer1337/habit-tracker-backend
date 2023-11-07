import * as cdk from 'aws-cdk-lib';
import { AttributeType, BillingMode, Table } from 'aws-cdk-lib/aws-dynamodb';
import { Construct } from 'constructs';
import { StageConfiguration } from './stage-configurations';
import { AccountRecovery, OAuthScope, ProviderAttribute, ResourceServerScope, UserPool, UserPoolClientIdentityProvider, UserPoolIdentityProviderGoogle } from 'aws-cdk-lib/aws-cognito';
import { Code, Function, Runtime } from 'aws-cdk-lib/aws-lambda';
import { CognitoUserPoolsAuthorizer, LambdaIntegration, RestApi } from 'aws-cdk-lib/aws-apigateway';
import { DnsValidatedCertificate } from 'aws-cdk-lib/aws-certificatemanager';
import { ARecord, HostedZone, RecordTarget } from 'aws-cdk-lib/aws-route53';
import { ApiGateway } from 'aws-cdk-lib/aws-route53-targets';
import { apexDomain, projectName } from './constants';

interface HabitTrackerBackendStackProps extends cdk.StackProps {
  stageConfig: StageConfiguration
}

export class HabitTrackerBackendStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props: HabitTrackerBackendStackProps) {
    super(scope, id, props);

    const stageConfig = props.stageConfig;
    const stageName = props.stageConfig.stageName;
    const apiSubDomain = `${stageConfig.stageName}.${projectName}.api`;
    const apiDomainName = `${apiSubDomain}.${apexDomain}`;

    const userPool = this.setupCognitoUserPool(projectName, props.stageConfig, apiDomainName);

    const table = new Table(this, `Table`, {
      tableName: `${projectName}-table-${this.region}-${stageName}`,
      partitionKey: { name: 'PK', type: AttributeType.STRING },
      sortKey: { name: 'SK', type: AttributeType.STRING },
      billingMode: BillingMode.PAY_PER_REQUEST,
      removalPolicy: cdk.RemovalPolicy.DESTROY
    });

    const lambdaFunction = new Function(this, 'LambdaFunction', {
      functionName: `${projectName}-api-${this.region}-${stageName}`,
      runtime: Runtime.DOTNET_6,
      code: Code.fromAsset(`../HabitTracker/HabitTracker/src/HabitTracker/bin/build-package.zip`),
      handler: 'HabitTracker',
      timeout: cdk.Duration.seconds(10),
      memorySize: 256,
      reservedConcurrentExecutions: 10,
      environment: {
        'TABLE_NAME': table.tableName,
        'USERINFO_ENDPOINT_URL': `https://${stageConfig.cognitoHostedUiDomainPrefix}.auth.${this.region}.amazoncognito.com/oauth2/userInfo`
      }
    });
    table.grantReadWriteData(lambdaFunction);
    const lambdaIntegration = new LambdaIntegration(lambdaFunction);

    const hostedZone = HostedZone.fromLookup(this, 'HostedZone', {
      domainName: apexDomain
    });
    const certificate = new DnsValidatedCertificate(this, 'Certificate', {
      domainName: apiDomainName,
      hostedZone: hostedZone,
      cleanupRoute53Records: true // not recommended for production use
    });
    const api = new RestApi(this, 'HabitTrackerRestAPI', {
      restApiName: `${projectName}-api-${this.region}-${stageName}`,
      domainName: {
        domainName: apiDomainName,
        certificate: certificate
      }
    });
    const proxyResource = api.root.addResource('{proxy+}');
    proxyResource.addMethod('ANY', lambdaIntegration, {
      authorizer: new CognitoUserPoolsAuthorizer(this, 'CognitoAuthorizer', {
        cognitoUserPools: [userPool]
      }),
      authorizationScopes: [`https://${apiDomainName}/*`]
    });
    /*
    The admin proxy resource and cors preflight are set explicitly to avoid the cors OPTIONS method 
    using the Cognito Authorizer, which would cause the cors preflight check to fail. Curiously enough,
    cors also has to be set in the .NET application.
    */
    proxyResource.addCorsPreflight({
      allowOrigins: stageConfig.corsOrigins
    });

    new ARecord(this, 'ARecord', {
      zone: hostedZone,
      recordName: apiSubDomain,
      target: RecordTarget.fromAlias(new ApiGateway(api)),
      ttl: cdk.Duration.seconds(0)
    });
  }

  private setupCognitoUserPool(projectName: string, envConfig: StageConfiguration, apiDomainName: string): UserPool {
    const stage = envConfig.stageName;
    const userPool = new UserPool(this, 'UserPool', {
      selfSignUpEnabled: false,
      userPoolName: `${projectName}-user-pool-${this.region}-${stage}`,
      signInAliases: { username: true, email: true },
      accountRecovery: AccountRecovery.EMAIL_ONLY,
      standardAttributes: {
        email: { required: true }
      },
      removalPolicy: cdk.RemovalPolicy.DESTROY
    });
    userPool.addDomain('UserPoolDomain', {
      cognitoDomain: {
        domainPrefix: envConfig.cognitoHostedUiDomainPrefix
      }
    });

    const fullAccessScope = new ResourceServerScope({ scopeName: '*', scopeDescription: 'Full access' });
    const resourceServer = userPool.addResourceServer('ResourceServer', {
      userPoolResourceServerName: 'Habit Tracker API',
      identifier: `https://${apiDomainName}`,
      scopes: [fullAccessScope]
    });

    const client = userPool.addClient('UserPoolAppClient', {
      supportedIdentityProviders: [
        UserPoolClientIdentityProvider.GOOGLE
      ],
      oAuth: {
        flows: {
          authorizationCodeGrant: true
        },
        callbackUrls: envConfig.corsOrigins,
        logoutUrls: envConfig.corsOrigins,
        scopes: [
          OAuthScope.resourceServer(resourceServer, fullAccessScope),
          OAuthScope.PROFILE,
          OAuthScope.EMAIL
        ]
      }
    });

    const googleProvider = new UserPoolIdentityProviderGoogle(this, 'UserPoolIdentityProviderGoogle', {
      userPool: userPool,
      // client ID and secret are replaced in the console.
      clientId: 'REPLACE-ME',
      clientSecret: 'REPLACE-ME',
      scopes: ['profile', 'email'],
      attributeMapping: {
        email: ProviderAttribute.GOOGLE_EMAIL
      }
    });
    client.node.addDependency(googleProvider);

    return userPool;
  }
}
