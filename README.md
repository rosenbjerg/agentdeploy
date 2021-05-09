# AgentDeploy

An agent for managing execution of [taco bell programming](http://widgetsandshit.com/teddziuba/2010/10/taco-bell-programming.html)-style deployment and server management scripts

[![CI](https://github.com/rosenbjerg/AgentDeploy/actions/workflows/ci.yml/badge.svg)](https://github.com/rosenbjerg/AgentDeploy/actions/workflows/ci.yml)
[![GitHub](https://img.shields.io/github/license/rosenbjerg/AgentDeploy)](https://github.com/rosenbjerg/AgentDeploy/blob/main/LICENSE)
[![Server Docker Pulls](https://img.shields.io/docker/pulls/mrosenbjerg/agentd-server?label=server%20docker%20pulls)](https://hub.docker.com/r/mrosenbjerg/agentd-server)
[![Client Docker Pulls](https://img.shields.io/docker/pulls/mrosenbjerg/agentd-client?label=client%20docker%20pulls)](https://hub.docker.com/r/mrosenbjerg/agentd-client)
[![codecov.io](https://codecov.io/github/rosenbjerg/AgentDeploy/coverage.svg?branch=main)](https://app.codecov.io/gh/rosenbjerg/AgentDeploy)



## Server
Running the `agentd` server, either in a container or directly using an executable, exposes a REST endpoint that can be used to invoke **scripts**. These **scripts** are either executed locally relative to the executable, or remotely through SSH.
**Scripts** are defined using yaml files that specify which *variables* the **script** require for invocation. Both *variables*, *secret variables* and *files* are declared and optionally constrained to certain values using regular expressions. *Files* can be constrained on size (maximum and minimum) and file extension.

Permission to invoke a **script** is granted through a **token**, which is also specified by a yaml file. A **token** can also limit which of the availabe **script**-files should be invocable with a given **token**. **Tokens** can also further constrain the input *variables* to a given **script** or even lock a *variable* to a specific value.

### Script examples
#### Minimal example 
```yaml
variables:
  name:
command: |
  echo "Hello $(name)!"
```

#### Advanced example
```yaml
variables:
  username:
  password:
    secret: true
files:
  test_file:
    max_size: 1_000
show_command: true
concurrency: none
command: |
  echo "logging in $(username):$(password)"
  cat $(test_file)
```


### Token example
#### Minimal example 
```yaml
available_scripts:
  minimal-example:
  advanced-example:
```

#### Advanced example 
```yaml
name: advanced example
description: just an advanced example with more stuff in it
ssh:
  username: myuser
  private_key_file: /home/myuser/.ssh/id_rsa
trusted_ips:
  - 127.0.0.1
  - ::1
  - ::ffff:172.18.0.1
available_scripts:
  minimal-example:
    variable_constraints: 
      name: ^john(doe)?$
  advanced-example:
    locked_variables:
      username: john
    variable_constraints:
      password ^[a-zA-Z0-9_-]{18,32}$
```

## Client
To simplify usage, the `agentd` CLI client can be used. The client provides functionality to utilize all functionality provided by the `agentd` server. This includes using WebSockets to allow for process output to be sent as soon as the process emits it, so it can be output in the CLI client.


#### Example
Basic example of invoking a script with the CLI client:
```
agentd-client invoke test-script http://localhost:5000 -i -v name=John
```


#### Help:
```
Usage: agentd-client [options] [command]

Options:
  -V, --version                                  output the version number
  -t, --token <token>                            Authentication token
  -i, --interactive                              Enable providing token through stdin
  -v, --variables <keyValuePair...>              Add variable
  -s, --secret-variables <keyValuePair...>       Add secret variable
  -e, --environment-variables <keyValuePair...>  Add environment variable
  -f, --files <keyValuePair...>                  Add file
  --ws                                           Enable websocket connection for receiving output
  --hide-timestamps                              Omit timestamps
  --hide-headers                                 Omit info headers
  --hide-script                                  Omit printing script (if available)
  -h, --help                                     display help for command

Commands:
  invoke <scriptName> <serverUrl>                Invoke named script on remote server
  help [command]                                 display help for command
```
