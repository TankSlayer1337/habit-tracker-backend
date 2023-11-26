import * as cdk from 'aws-cdk-lib';
import * as ecs from 'aws-cdk-lib/aws-ecs';
import * as ecsPatterns from 'aws-cdk-lib/aws-ecs-patterns';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import { AttributeType, BillingMode, Table } from 'aws-cdk-lib/aws-dynamodb';
import { Construct } from 'constructs';
import { StageConfiguration } from './stage-configurations';
import * as cognito from 'aws-cdk-lib/aws-cognito';
import { DnsValidatedCertificate } from 'aws-cdk-lib/aws-certificatemanager';
import { HostedZone } from 'aws-cdk-lib/aws-route53';
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

    const vpc = ec2.Vpc.fromLookup(this, 'DefaultVPC', {
      isDefault: true
    });

    const securityGroup = new ec2.SecurityGroup(this, 'SecurityGroup', {
      vpc,
      description: 'Habit Tracker Security Group',
      allowAllOutbound: true
    });
    securityGroup.addIngressRule(
      ec2.Peer.anyIpv4(),
      ec2.Port.tcp(80),
      'Allow HTTP traffic from anywhere'
    );
    
    const hostedZone = HostedZone.fromLookup(this, 'HostedZone', {
      domainName: apexDomain
    });
    const certificate = new DnsValidatedCertificate(this, 'Certificate', {
      domainName: apiDomainName,
      hostedZone: hostedZone,
      cleanupRoute53Records: true // not recommended for production use
    });

    const loadBalancedFargateService = new ecsPatterns.ApplicationLoadBalancedFargateService(this, 'Service', {
      vpc,
      securityGroups: [securityGroup],
      cpu: 256,
      memoryLimitMiB: 512,
      desiredCount: 1,
      taskImageOptions: {
        image: ecs.ContainerImage.fromAsset('../HabitTracker'),
        containerPort: 8080,
        environment: {
          'TABLE_NAME': table.tableName,
          'USERINFO_ENDPOINT_URL': `https://${stageConfig.cognitoHostedUiDomainPrefix}.auth.${this.region}.amazoncognito.com/oauth2/userInfo`
        }
      },
      publicLoadBalancer: true,
      assignPublicIp: true,
      certificate,
      domainName: apiDomainName,
      domainZone: hostedZone,
      recordType: ecsPatterns.ApplicationLoadBalancedServiceRecordType.ALIAS
    });

    table.grantReadWriteData(loadBalancedFargateService.taskDefinition.taskRole);
  }

  private setupCognitoUserPool(projectName: string, envConfig: StageConfiguration, apiDomainName: string): cognito.UserPool {
    const stage = envConfig.stageName;
    const userPool = new cognito.UserPool(this, 'UserPool', {
      selfSignUpEnabled: false,
      userPoolName: `${projectName}-user-pool-${this.region}-${stage}`,
      signInAliases: { username: true, email: true },
      accountRecovery: cognito.AccountRecovery.EMAIL_ONLY,
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

    const fullAccessScope = new cognito.ResourceServerScope({ scopeName: '*', scopeDescription: 'Full access' });
    const resourceServer = userPool.addResourceServer('ResourceServer', {
      userPoolResourceServerName: 'Habit Tracker API',
      identifier: `https://${apiDomainName}`,
      scopes: [fullAccessScope]
    });

    const client = userPool.addClient('UserPoolAppClient', {
      supportedIdentityProviders: [
        cognito.UserPoolClientIdentityProvider.GOOGLE
      ],
      oAuth: {
        flows: {
          authorizationCodeGrant: true
        },
        callbackUrls: envConfig.corsOrigins,
        logoutUrls: envConfig.corsOrigins,
        scopes: [
          cognito.OAuthScope.resourceServer(resourceServer, fullAccessScope),
          cognito.OAuthScope.PROFILE,
          cognito.OAuthScope.EMAIL
        ]
      }
    });

    const googleProvider = new cognito.UserPoolIdentityProviderGoogle(this, 'UserPoolIdentityProviderGoogle', {
      userPool: userPool,
      // client ID and secret are replaced in the console.
      clientId: 'REPLACE-ME',
      clientSecret: 'REPLACE-ME',
      scopes: ['profile', 'email'],
      attributeMapping: {
        email: cognito.ProviderAttribute.GOOGLE_EMAIL
      }
    });
    client.node.addDependency(googleProvider);

    return userPool;
  }
}
