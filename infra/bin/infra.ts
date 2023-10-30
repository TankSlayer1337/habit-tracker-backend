#!/usr/bin/env node
import 'source-map-support/register';
import * as cdk from 'aws-cdk-lib';
import { stageConfigurations } from '../lib/stage-configurations';
import { HabitTrackerBackendStack } from '../lib/habit-tracker-backend-stack';
import { projectName } from '../lib/constants';

const app = new cdk.App();
stageConfigurations.forEach(stageConfig => {
  const env = stageConfig.awsEnv;
  new HabitTrackerBackendStack(app, `${projectName}-backend-${env.region}-${stageConfig.stageName}`, {
    stageConfig: stageConfig,
    env: env
  });
});