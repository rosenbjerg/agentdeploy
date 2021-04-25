# AgentDeploy

An agent for managing execution of [taco bell programming](http://widgetsandshit.com/teddziuba/2010/10/taco-bell-programming.html) style deployment and server management scripts

[![CI](https://github.com/rosenbjerg/AgentDeploy/actions/workflows/ci.yml/badge.svg)](https://github.com/rosenbjerg/AgentDeploy/actions/workflows/ci.yml)

### How does it work then?
You run the `agentd` service, either in a container or directly using an executable. This service exposes a REST endpoint that can be used to invoke **scripts** through ssh or locally.
These **scripts** are defined using yaml files that which *variables* the **script** takes, which files if any and constraints for *variables* and *files*.
Permission to invoke a **script** is granted through a **token**, which is also a yaml file, optionally limited which of the **script**-files should be available for a given **token**.
Tokens can also further constrain the input *variables* to a given **script** or even lock a *variable* to a specific value.
