[![Build Status](https://travis-ci.org/afractal/EnvironmentVariableProvider.svg?branch=master)](https://travis-ci.org/afractal/EnvironmentVariableProvider)
[![Build status](https://ci.appveyor.com/api/projects/status/9lbmt83rfwmj1ln9?svg=true)](https://ci.appveyor.com/project/hermesxgjini/environmentvariableprovider)


## **Environment Variable Type Provider**

Type provider for getting environment variables at design time.


## **Getting started**

To get started, just instantiate the type provider and pass in an environment variable flag as the parameter.

```fsharp
open FSharp.Environment.TypeProviders
open FSharp.Environment

type EnvVars = EnvironmentVariableProvider< target = EnvironmentTarget.Machine  >

let envVars = EnvVars()
```

The variable **envVars**  now contains the list of env variables.





