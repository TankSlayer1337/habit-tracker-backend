import { Environment } from "aws-cdk-lib"
import { stockholm } from "./constants"

// rename to StageConfiguration
export interface StageConfiguration {
  awsEnv: Environment,
  stageName: string,
  stageSubDomain: string,
  corsOrigins: string[],
  cognitoHostedUiDomainPrefix: string
}

export const devConfiguration: StageConfiguration = {
  awsEnv: stockholm,
  stageName: 'dev',
  stageSubDomain: 'dev.',
  corsOrigins: [
    'http://localhost:5173',
    'https://dev.habit-tracker.cloudchaotic.com'
  ],
  cognitoHostedUiDomainPrefix: 'habit-tracker-dev'
}

export const prodConfiguration: StageConfiguration = {
  awsEnv: stockholm,
  stageName: 'prod',
  stageSubDomain: 'prod.',
  corsOrigins: [ 'https://habit-tracker.cloudchaotic.com' ],
  cognitoHostedUiDomainPrefix: 'habit-tracker-prod'
}

export const stageConfigurations: StageConfiguration[] = [ devConfiguration, prodConfiguration ]