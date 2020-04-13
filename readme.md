# Non-Persistent Objects Editing Demo

This example demonstrates a possible implementation of editable non-persistent objects that represent data stored remotely and separate from the main XAF application database. Here we use a simplified [IDataStore](https://docs.devexpress.com/CoreLibraries/DevExpress.Xpo.DB.IDataStore) implementation.

These non-persistent objects can be created, deleted, and modified. Their changes are persisted in the external storage. The **FilterController** is enabled for these objects, so their list and lookup list views can be filtered. The built-in **IsNewObject** function is used in a criterion of an appearance rule that disabled the key property editor after the Account object is saved.

The following [NonPersistentObjectSpace](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.NonPersistentObjectSpace) members are used in this example.

Non-persistent objects are kept in an object map. In the [ObjectsGetting](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.NonPersistentObjectSpace.ObjectsGetting?v=20.1), [ObjectGetting](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.NonPersistentObjectSpace.ObjectGetting), and [ObjectByKeyGetting](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.NonPersistentObjectSpace.ObjectByKeyGetting) event handlers, non-persistent objects are looked up and added to the object map. In the [Reloaded](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.BaseObjectSpace.Reloaded) event handler, the object map is cleared. So, subsequent object queries trigger the creation of new non-persistent object instances. In the [ObjectReloading](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.NonPersistentObjectSpace.ObjectReloading?v=20.1) event handler, the state of an existing object is reloaded from the storage. 

In the [CustomCommitChanges](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.BaseObjectSpace.CustomCommitChanges?v=20.1) event handler, all object changes are processed and passed to the storage in a single atomic operation.

The [NonPersistentObjectSpace\.NeedSetModifiedOnObjectChanged](https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.NonPersistentObjectSpace.NeedSetModifiedOnObjectChanged?v=20.1) property is set to *true* to automatically mark non-persistent objects as modified when they are changed and raise the INotifyPropertyChanged.PropertyChanged notification.


## Common Components

The following classes are used to provide common functionality for all non-persistent objects used in the demo.

### NonPersistentObjectBase

The abstract base class for all non-persistent objects used in the application. It provides a common implementation of the INotifyPropertyChanged and IObjectSpaceLink interfaces and some useful protected methods.

### NonPersistentObjectSpaceHelper

This is a heper class that subscribes to the XafApplication.ObjectSpaceCreated event, and performs a common setup of NPOS. This usually includes creating and linking additional object spaces, and setting up object adapters. Adapters are registered on the Application.SetupComplete event in the module code.

### TransientNonPersistentObjectAdapter

The adapter for transient (short-living) NP business objects. A new adapter instance is created for each non-persistent object space. It subscribes to object space events to manage a subset of object types in a common manner. It uses a factory to handle specific object types and their storage. It also maintains an identity map (ObjectMap) for NonPersistentObjectSpace.

### NonPersistentObjectFactoryBase

Descendants of this class know how to create object instances and transfer data between objects and the storage. It knows nothing about the adapter. It also uses the identity map to avoid creating duplicated objects.

### DataStoreMapper

This is a set of classes that represent an external storage. These classes can be used by a factory to store object data in XPO's IDataStore storages. The classes are:
- DataStoreObjectLoader - loads objects by their keys and collections of objects.
- DataStoreObjectSaver - saves modified objects' data.
- DataStoreMapping - describes how objects are mapped to tables and columns.

