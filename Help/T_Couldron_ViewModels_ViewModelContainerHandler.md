# ViewModelContainerHandler Class
 _**\[This is preliminary documentation and is subject to change.\]**_

Handles the children of viewmodels


## Inheritance Hierarchy
System.Object<br />&nbsp;&nbsp;<a href="T_Couldron_Core_DisposableBase">Couldron.Core.DisposableBase</a><br />&nbsp;&nbsp;&nbsp;&nbsp;Couldron.ViewModels.ViewModelContainerHandler<br />
**Namespace:**&nbsp;<a href="N_Couldron_ViewModels">Couldron.ViewModels</a><br />**Assembly:**&nbsp;Couldron (in Couldron.dll) Version: 1.0.0.0 (1.0.0.0)

## Syntax

**C#**<br />
``` C#
public sealed class ViewModelContainerHandler : DisposableBase
```

The ViewModelContainerHandler type exposes the following members.


## Constructors
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_Couldron_ViewModels_ViewModelContainerHandler__ctor">ViewModelContainerHandler</a></td><td>
Initializes a new instance of ViewModelContainerHandler class</td></tr></table>&nbsp;
<a href="#viewmodelcontainerhandler-class">Back to Top</a>

## Properties
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_Couldron_Core_DisposableBase_IsDisposed">IsDisposed</a></td><td>
Gets a value indicating if the object has been disposed or not
 (Inherited from <a href="T_Couldron_Core_DisposableBase">DisposableBase</a>.)</td></tr><tr><td>![Public property](media/pubproperty.gif "Public property")</td><td><a href="P_Couldron_ViewModels_ViewModelContainerHandler_Parent">Parent</a></td><td>
Gets the parent of the object</td></tr></table>&nbsp;
<a href="#viewmodelcontainerhandler-class">Back to Top</a>

## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_Couldron_Core_DisposableBase_Dispose">Dispose()</a></td><td>
Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
 (Inherited from <a href="T_Couldron_Core_DisposableBase">DisposableBase</a>.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="M_Couldron_Core_DisposableBase_Dispose_1">Dispose(Boolean)</a></td><td>
Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
 (Inherited from <a href="T_Couldron_Core_DisposableBase">DisposableBase</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td>Equals</td><td> (Inherited from Object.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="M_Couldron_Core_DisposableBase_Finalize">Finalize</a></td><td>
Destructors are used to destruct instances of classes.
 (Inherited from <a href="T_Couldron_Core_DisposableBase">DisposableBase</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td>GetHashCode</td><td> (Inherited from Object.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_Couldron_ViewModels_ViewModelContainerHandler_GetRegistered">GetRegistered(Guid)</a></td><td>
Returns a registered child viewmodel</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_Couldron_ViewModels_ViewModelContainerHandler_GetRegistered__1">GetRegistered(T)()</a></td><td>
Returns a registered Child ViewModel</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td>GetType</td><td> (Inherited from Object.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td>MemberwiseClone</td><td> (Inherited from Object.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="M_Couldron_ViewModels_ViewModelContainerHandler_OnDispose">OnDispose</a></td><td>
Occures after Dispose() has been invoked
 (Overrides <a href="M_Couldron_Core_DisposableBase_OnDispose">DisposableBase.OnDispose(Boolean)</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_Couldron_ViewModels_ViewModelContainerHandler_Register">Register</a></td><td>
Registers a child model to the current ViewModel</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td>ToString</td><td> (Inherited from Object.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_Couldron_ViewModels_ViewModelContainerHandler_UnRegister_1">UnRegister(Guid)</a></td><td>
Unregisters a registered viewmodel. This will also dispose the viewmodel.</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_Couldron_ViewModels_ViewModelContainerHandler_UnRegister">UnRegister(IViewModel)</a></td><td>
Unregisters a registered viewmodel. This will also dispose the viewmodel.</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="M_Couldron_ViewModels_ViewModelContainerHandler_Validate">Validate</a></td><td>
Starts a validation on all properties</td></tr></table>&nbsp;
<a href="#viewmodelcontainerhandler-class">Back to Top</a>

## Events
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public event](media/pubevent.gif "Public event")</td><td><a href="E_Couldron_ViewModels_ViewModelContainerHandler_Changed">Changed</a></td><td>
Occures when a value has changed</td></tr><tr><td>![Public event](media/pubevent.gif "Public event")</td><td><a href="E_Couldron_Core_DisposableBase_Disposed">Disposed</a></td><td>
Occures if the object has been disposed
 (Inherited from <a href="T_Couldron_Core_DisposableBase">DisposableBase</a>.)</td></tr></table>&nbsp;
<a href="#viewmodelcontainerhandler-class">Back to Top</a>

## Extension Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public Extension Method](media/pubextension.gif "Public Extension Method")</td><td><a href="M_Couldron_Extensions_CastTo__1">CastTo(T)</a></td><td>
Performs certain types of conversions between compatible reference types or nullable types 

 Returns null if convertion was not successfull
 (Defined by <a href="T_Couldron_Extensions">Extensions</a>.)</td></tr><tr><td>![Public Extension Method](media/pubextension.gif "Public Extension Method")</td><td><a href="M_Couldron_Extensions_DisposeAll">DisposeAll</a></td><td>
Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. 

 This will dispose an object if it implements the IDisposable interface. 

 If the object is a FrameworkElement it will try to find known diposable attached properties. 

 It will also dispose the the DataContext content.
 (Defined by <a href="T_Couldron_Extensions">Extensions</a>.)</td></tr><tr><td>![Public Extension Method](media/pubextension.gif "Public Extension Method")</td><td><a href="M_Couldron_ExtensionConvertions_ToBool">ToBool</a></td><td>
Tries to convert an Object to a Boolean
 (Defined by <a href="T_Couldron_ExtensionConvertions">ExtensionConvertions</a>.)</td></tr><tr><td>![Public Extension Method](media/pubextension.gif "Public Extension Method")</td><td><a href="M_Couldron_ExtensionConvertions_ToDouble">ToDouble</a></td><td>
Tries to convert a Object to an Double
 (Defined by <a href="T_Couldron_ExtensionConvertions">ExtensionConvertions</a>.)</td></tr><tr><td>![Public Extension Method](media/pubextension.gif "Public Extension Method")</td><td><a href="M_Couldron_ExtensionConvertions_ToInteger">ToInteger</a></td><td>
Tries to convert a Object to an Int32
 (Defined by <a href="T_Couldron_ExtensionConvertions">ExtensionConvertions</a>.)</td></tr><tr><td>![Public Extension Method](media/pubextension.gif "Public Extension Method")</td><td><a href="M_Couldron_ExtensionConvertions_ToString2">ToString2</a></td><td>
Returns a string that represents the current object. 

 If the object is null a Empty will be returned
 (Defined by <a href="T_Couldron_ExtensionConvertions">ExtensionConvertions</a>.)</td></tr></table>&nbsp;
<a href="#viewmodelcontainerhandler-class">Back to Top</a>

## See Also


#### Reference
<a href="N_Couldron_ViewModels">Couldron.ViewModels Namespace</a><br />