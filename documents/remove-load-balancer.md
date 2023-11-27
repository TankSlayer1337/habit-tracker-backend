It seems like ECS services running on Fargate does not support Elastic IP:
https://repost.aws/knowledge-center/ecs-fargate-static-elastic-ip-address
https://stackoverflow.com/questions/47894034/how-do-i-associate-an-elastic-ip-with-a-fargate-container

The easiest way to get rid of the load balancer might be to host the container on an EC2 instance instead of on Fargate, and assign an Elastic IP to it.

This answer suggests it is possible to use an auto scaling group with always only 1 instance along with an elastic IP: "AutoScaling only supports a static EIP when the group has a max of 1 instance (in which case that EIP/ENI can be reused on replacement instance, but the group can't scale above 1 instance)." [link](https://repost.aws/questions/QUE0n02DLQRvOe-Uswma3sqA/how-to-allocate-elastic-ip-address-to-auto-scaling-group#ANBIXulip9Rwyt68eji8Eqew)
