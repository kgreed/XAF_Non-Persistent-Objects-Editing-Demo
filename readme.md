# Non-Persistent Objects Demo


## Common Components

### NonPersistentObjectBase

The abstract base class for all non-persistent objects used in the application. It provides a common implementation of the INotifyPropertyChanged and IObjectSpaceLink interfaces and some useful protected methods.

### NonPersistentObjectSpaceHelper

This is a heper class that subscribes to the XafApplication.ObjectSpaceCreated event, and performs a common setup of NPOS. This usually includes creating and linking additional object spaces, and setting up object adapters. Adapters are registered on the Application.SetupComplete event in the module code.

### TransientNonPersistentObjectAdapter

The adapter for transient (short-living) NP business objects. A new adapter instance is created for each non-persistent object space. It subscribes to object space events to manage a subset of object types in a common manner. It uses a factory to handle specific object types and their storage. It also maintains an identity map (ObjectMap) for NPOS.

### NonPersistentObjectFactoryBase

This class knows how to creates object instances and transfer data between objects and the storage. It knows nothing about the adapter. It also uses the identity map to avoid creating duplicated objects.

### DataStoreMapper

This is a set of classes that represent an external storage example. These classes can be used by a factory to store object data in XPO's IDataStore storages. The classes are:
- DataStoreObjectLoader - loads objects by their keys and collections of objects.
- DataStoreObjectSaver - saves modified objects' data.
- DataStoreMapping - describes how objects are mapped to tables and columns.


## Demos

### The Quick Brows Fox

This part demonstrates an implementation of a non-persitent object (LiveReport) that represents aggregated results for a group of persistent objects, with calculated scalar and persitent reference properties, and a drill-down collection. The non-persitent class is not editable and its storage is read-only. Persistent objects linked to LiveReport can be opened and edited in separate detail views. The non-persitent object views can be refreshed to show actual data by reloading linked persistent objects.

### Post Office

This part demonstrates an implementation of editable non-persistent objects (Account and Message) that represent entities obtained from an external service (here, InMemoryDataStore is used to keep object data). Objects can be created, deleted and modified, and their changes persisted in the external storage. The FilterController is enabled for these objects, so thir list and lookup list views can be filtered.

