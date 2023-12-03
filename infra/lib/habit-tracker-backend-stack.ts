import * as cdk from 'aws-cdk-lib';
import * as ecs from 'aws-cdk-lib/aws-ecs';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import { AttributeType, BillingMode, Table } from 'aws-cdk-lib/aws-dynamodb';
import { Construct } from 'constructs';
import { StageConfiguration } from './stage-configurations';
import * as cognito from 'aws-cdk-lib/aws-cognito';
import { ARecord, HostedZone, RecordTarget } from 'aws-cdk-lib/aws-route53';
import { apexDomain, projectName } from './constants';
import { ManagedPolicy, Role, ServicePrincipal } from 'aws-cdk-lib/aws-iam';
import { AutoScalingGroup, CfnAutoScalingGroup } from 'aws-cdk-lib/aws-autoscaling';
import { LogGroup, RetentionDays } from 'aws-cdk-lib/aws-logs';
import { Bucket } from 'aws-cdk-lib/aws-s3';

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
    securityGroup.addIngressRule(
      ec2.Peer.anyIpv4(),
      ec2.Port.tcp(443),
      'Allow HTTPS traffic from anywhere'
    );

    const subnet = vpc.publicSubnets.sort(this.compareIds)[0];

    const elasticIp = new ec2.CfnEIP(this, 'ElasticIp');
    elasticIp.applyRemovalPolicy(cdk.RemovalPolicy.DESTROY);
    const networkInterface = new ec2.CfnNetworkInterface(this, 'NetworkInterface', {
      subnetId: subnet.subnetId,
      groupSet: [ securityGroup.securityGroupId ]
    });
    const eipAssociation = new ec2.CfnEIPAssociation(this, 'EIPAssociation', {
      allocationId: elasticIp.attrAllocationId,
      networkInterfaceId: networkInterface.attrId
    });
    eipAssociation.applyRemovalPolicy(cdk.RemovalPolicy.DESTROY);

    const instanceRole = new Role(this, 'InstanceRole', {
      assumedBy: new ServicePrincipal('ec2.amazonaws.com')
    });
    instanceRole.addManagedPolicy(ManagedPolicy.fromAwsManagedPolicyName('service-role/AmazonEC2ContainerServiceforEC2Role'));

    const cluster = new ecs.Cluster(this, 'Cluster', {
      vpc
    });

    const userData = ec2.UserData.forLinux();
    userData.addCommands(`echo "ECS_CLUSTER=${cluster.clusterName}" >> /etc/ecs/ecs.config`);
    const launchTemplate = new ec2.LaunchTemplate(this, 'LaunchTemplate', {
      instanceType: ec2.InstanceType.of(ec2.InstanceClass.T3, ec2.InstanceSize.NANO),
      machineImage: ecs.EcsOptimizedImage.amazonLinux2023(),
      role: instanceRole,
      userData
    });
    // L2 construct does not yet support specifying network interfaces: https://github.com/aws/aws-cdk/issues/14494
    // use cdk escape hatch in order to set network interface
    const cfnLaunchTemplate = launchTemplate.node.defaultChild as ec2.CfnLaunchTemplate;
    cfnLaunchTemplate.launchTemplateData = {
      ...cfnLaunchTemplate.launchTemplateData,
      networkInterfaces: [{
        deleteOnTermination: false,
        deviceIndex: 0,
        networkInterfaceId: networkInterface.attrId
      }]
    };

    const autoScalingGroup = new AutoScalingGroup(this, 'AutoScalingGroup', {
      vpc,
      launchTemplate,
      minCapacity: 1,
      maxCapacity: 1
    });
    const cfnAutoScalingGroup = autoScalingGroup.node.defaultChild as CfnAutoScalingGroup;
    cfnAutoScalingGroup.availabilityZones = [ subnet.availabilityZone ];
    // cannot specify subnet ID if setting existing network interface ID.
    cfnAutoScalingGroup.vpcZoneIdentifier = undefined;

    const capacityProvider = new ecs.AsgCapacityProvider(this, 'AsgCapacityProvider', {
      autoScalingGroup
    });
    cluster.addAsgCapacityProvider(capacityProvider);

    const s3Bucket = new Bucket(this, 'Bucket', {
      removalPolicy: cdk.RemovalPolicy.DESTROY
    });

    const logGroup = new LogGroup(this, 'LogGroup', {
      logGroupName: `habit-tracker-service-${stageName}`,
      retention: RetentionDays.ONE_MONTH,
      removalPolicy: cdk.RemovalPolicy.DESTROY
    });
    const taskDefinition = new ecs.Ec2TaskDefinition(this, 'TaskDefinition');
    taskDefinition.addContainer('HabitTrackerWebAPIContainer', {
      image: ecs.ContainerImage.fromAsset('../HabitTracker'),
      portMappings: [ { containerPort: 8080, hostPort: 80 }, { containerPort: 8081, hostPort: 443 } ],
      memoryReservationMiB: 128,
      environment: {
        'TABLE_REGION': table.env.region,
        'TABLE_NAME': table.tableName,
        'USERINFO_ENDPOINT_URL': `https://${stageConfig.cognitoHostedUiDomainPrefix}.auth.${this.region}.amazoncognito.com/oauth2/userInfo`,
        'BUCKET_REGION': s3Bucket.env.region,
        'BUCKET_NAME': s3Bucket.bucketName,
        'DOMAIN_NAME': apiDomainName,
        'CORS_ORIGINS': stageConfig.corsOrigins.join()
      },
      logging: ecs.LogDrivers.awsLogs({
        streamPrefix: `habit-tracker-service-${stageName}`,
        logGroup
      })
    });
    table.grantReadWriteData(taskDefinition.taskRole);
    s3Bucket.grantReadWrite(taskDefinition.taskRole);

    const service = new ecs.Ec2Service(this, 'EC2Service', {
      cluster,
      taskDefinition,
      desiredCount: 1,
      maxHealthyPercent: 100,
      minHealthyPercent: 0,
      circuitBreaker: { rollback: true }
    });

    const hostedZone = HostedZone.fromLookup(this, 'HostedZone', {
      domainName: apexDomain
    });

    const aRecord = new ARecord(this, 'ARecord', {
      target: RecordTarget.fromIpAddresses(elasticIp.attrPublicIp),
      zone: hostedZone,
      recordName: apiSubDomain,
      ttl: cdk.Duration.minutes(30)
    });
    aRecord.applyRemovalPolicy(cdk.RemovalPolicy.DESTROY);
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

  private compareIds(a: ec2.ISubnet, b: ec2.ISubnet): number {
    if ( a.subnetId < b.subnetId ){
      return -1;
    }
    if ( a.subnetId > b.subnetId ){
      return 1;
    }
    return 0;
  }
}
