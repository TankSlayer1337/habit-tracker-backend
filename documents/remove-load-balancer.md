It seems like ECS services running on Fargate does not support Elastic IP:
https://repost.aws/knowledge-center/ecs-fargate-static-elastic-ip-address
https://stackoverflow.com/questions/47894034/how-do-i-associate-an-elastic-ip-with-a-fargate-container

The easiest way to get rid of the load balancer might be to host the container on an EC2 instance instead of on Fargate, and assign an Elastic IP to it.

This answer suggests it is possible to use an auto scaling group with always only 1 instance along with an elastic IP: "AutoScaling only supports a static EIP when the group has a max of 1 instance (in which case that EIP/ENI can be reused on replacement instance, but the group can't scale above 1 instance)." [link](https://repost.aws/questions/QUE0n02DLQRvOe-Uswma3sqA/how-to-allocate-elastic-ip-address-to-auto-scaling-group#ANBIXulip9Rwyt68eji8Eqew)

## Difference between ECS Roles
**Container Instance IAM Role**: The role assigned to the EC2 instance that your ECS tasks will be deployed to. This role is not used if you are deploying to Fargate. The description of this role is:  
Amazon ECS attaches this policy to a service role that allows Amazon ECS to perform actions on your behalf against Amazon EC2 instances or external instances.  
This role is used by the EC2 instances to register/join the ECS cluster.

**Task execution role**: Used by the ECS service to do things like pull the image from ECR and send container logs to CloudWatch.

**Task role**: This role is optional. It is the role your application code running in the ECS task container can assume to make AWS API calls.