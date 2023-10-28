import { Environment } from "aws-cdk-lib";

export const repositoryName = 'habit-tracker-backend';
export const stockholm: Environment = { region: 'eu-north-1', account: process.env.CDK_DEFAULT_ACCOUNT };