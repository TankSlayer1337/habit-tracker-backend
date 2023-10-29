import { Environment } from "aws-cdk-lib"
import { apexDomain, projectName } from "./constants"

export interface StageConfiguration {
  awsEnv: Environment,
  stageName: string,
  corsOrigins: string[],
  cognitoHostedUiDomainPrefix: string
}

const stockholm: Environment = { region: 'eu-north-1', account: process.env.CDK_DEFAULT_ACCOUNT };
const baseDomain = `${projectName}.${apexDomain}`;

export const devConfiguration: StageConfiguration = {
  awsEnv: stockholm,
  stageName: 'dev',
  corsOrigins: [
    'http://localhost:5173',
    `https://dev.${baseDomain}`
  ],
  cognitoHostedUiDomainPrefix: `${projectName}-dev`
}

export const prodConfiguration: StageConfiguration = {
  awsEnv: stockholm,
  stageName: 'prod',
  corsOrigins: [ `https://${baseDomain}` ],
  cognitoHostedUiDomainPrefix: `${projectName}-prod`
}

export const stageConfigurations: StageConfiguration[] = [ devConfiguration, prodConfiguration ]