# stashbox [![Build status](https://ci.appveyor.com/api/projects/status/0849ee6awjyxohei/branch/master?svg=true)](https://ci.appveyor.com/project/pcsajtai/stashbox/branch/master) [![Coverage Status](https://coveralls.io/repos/z4kn4fein/stashbox/badge.svg?branch=master&service=github)](https://coveralls.io/github/z4kn4fein/stashbox?branch=master) [![Join the chat at https://gitter.im/z4kn4fein/stashbox](https://img.shields.io/badge/gitter-join%20chat-green.svg)](https://gitter.im/z4kn4fein/stashbox?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) [![NuGet Version](http://img.shields.io/nuget/v/Stashbox.svg?style=flat)](https://www.nuget.org/packages/Stashbox/) [![NuGet Downloads](http://img.shields.io/nuget/dt/Stashbox.svg?style=flat)](https://www.nuget.org/packages/Stashbox/)
Stashbox is a lightweight, portable dependency injection framework for .NET based solutions.

##Features

 - *Fluent interface* - for faster and easier configuration.
 - *Child container* - scopes can be managed with child containers with automatic fallback to parent when required.
 - *Lifetime scopes* - including singleton and transient scope, custom lifetimes also can be used.
 - *Conditional resolution* - attribute and parent-type based conditions can be specified.
 - *IDisposable object tracking* - disposable objects are being disposed by the container when needed.
 - *Special types* - including IEnumerable, arrays, `Lazy<T>` and open generic types.
 - *Custom resolvers* - the built-in resolution operations can be extended by custom resolvers.
 - *Container extensions* - the functionality of the container can be extended by custom extensions.

##Supported platforms

 - .NET 4.5 and above
 - Windows 8/8.1/10
 - Windows Phone Silverlight 8/8.1
 - Windows Phone 8.1
 - Xamarin (Android/iOS/iOS Classic)

## Sample usage
```c#
class Wulfgar : IBarbarian
{
    public Wulfgar(IWeapon weapon)
    {
        //...
    }
}

container.RegisterType<IWeapon, AegisFang>();
container.RegisterType<IBarbarian, Wulfgar>();

var wulfgar = container.Resolve<IBarbarian>();
```
##Documentation
 - [Wiki](https://github.com/z4kn4fein/stashbox/wiki)