# AgentDeploy

An agent for managing execution of [taco bell programming](http://widgetsandshit.com/teddziuba/2010/10/taco-bell-programming.html) style deployment and server management scripts

[![CI](https://github.com/rosenbjerg/AgentDeploy/actions/workflows/ci.yml/badge.svg)](https://github.com/rosenbjerg/AgentDeploy/actions/workflows/ci.yml)
[![GitHub](https://img.shields.io/github/license/rosenbjerg/AgentDeploy)](https://github.com/rosenbjerg/AgentDeploy/blob/main/LICENSE)
[![Server Docker Pulls](https://img.shields.io/docker/pulls/mrosenbjerg/agentd-server?label=server%20docker%20pulls)](https://hub.docker.com/r/mrosenbjerg/agentd-server)
[![Client Docker Pulls](https://img.shields.io/docker/pulls/mrosenbjerg/agentd-client?label=client%20docker%20pulls)](https://hub.docker.com/r/mrosenbjerg/agentd-client)
[![codecov.io](https://codecov.io/github/rosenbjerg/AgentDeploy/coverage.svg?branch=main)](https://app.codecov.io/gh/rosenbjerg/AgentDeploy)



### How does it work?
You run the `agentd` server, either in a container or directly using an executable. This server exposes a REST endpoint that can be used to invoke **scripts** that are executed locally, or remotely through SSH.
These **scripts** are defined using yaml files that specify which *variables* the **script** require for invocation. Both *variables*, *secret variables* and *files* are declared and optionally constrained to certain values using regular expressions. *Files* can be constrained on size (maximum and minimum) and file extension.

Permission to invoke a **script** is granted through a **token**, which is also a yaml file. A **token** can also limit which of the availabe **script**-files should be invocable for a given **token**, and can also further constrain the input *variables* to a given **script** or even lock a *variable* to a specific value.
