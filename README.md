## Build Instructions
Ensure that .net 8 **and** .net 6 is installed and ensure that your project and solution settings have .net 8 selected as the target framework.

Run the command to install the effects build tool `dotnet tool install dotnet-mgcb-compute`

Open project in supported IDE and run.
* Jetbrains Rider: Opens without any extra work needed.
* Visual Studio Community for C#: Opens and builds without any extra work needed.
* Visual Studio Code: Can open project as a folder, but you will need to install the C# plugin. This is not the recommended way as the debugging tools aren't as well developed as they are with Rider or Visual Studio Community.

*Currently the project will not build on windows due to a bug in the MonoGame.Content.Builder.Task.Compute nuget package. If you are unable to build the project, please first try uninstalling that as it's currently unused.*

## ECS:
An ecs is partially implemented right now for loading and processing entities and systems.
### Entities:
Entities are just the index where the entity exists in the Entities list.
When entities are deleted, all of their corresponding components are dereferenced by each component array that the entity exists in. The index for the deleted entity is added.

You can create an Entity and retrieve it with the following two lines:

    EntityManager.AddEntity();
    int entity = EntityManager.LastAddedEntity;
or

    int entity = EntityManager.AddEntity();

### Components:
Components hold the data for each entity if that entity is assigned a component.
Components are automatically loaded if the definition of the component exists in the Components folder.
Any object that is not in the component folder can be added manually to the component manager. It is preferred to create the component by automatically loading it through the component manager, but if an argument can be made as to why it shouldn't have systems associated with it, it will probably be okay to load manually as a full class with its own methods.
* A few examples of current objects added manually: Terrain.cs, CameraControls.cs, MeshCollider.cs, RoadMesh.cs -> These classes are very large and were mostly implemented before switching to an ECS, but are also added as global entities. Any component that will exist more than once should never be loaded manually.

The 0th index of the entity manager is reserved as the global component entity. This is used for some components that will only ever exist once.

#### Retrieving Components:
You can retrieve a list of entities with a specific component with the following code:

    List<int> entities = ComponentManager.GetComponent<DrawableAsset>();
    int componentID = ComponentManager.GetComponentID<DrawableAsset>();
    for (int entity in entities)
    {
        DrawableAsset = entities[i][componentID]
        //do something with each DrawableAsset
    }

You can also retrieve an entity's components with:

    Object[] EntityComponents = EntityManager.EntityComponents[entity];

To retreive a specific component for a specific entity, you can typecast the object with something like the following:

    int zoneComponent = ComponentManager.GetComponentID<Zone>();
    Zone zone = (Zone)EntityManager.EntityComponents[entity][zoneComponent];

or

    Zone zone = ComponentManager.GetEntityComponent<Zone>(entityID)
    //replace Zone with the component you want to retrieve

You can also retrieve a global component by only specifying the type since these are reserved in the 0th index of the entities:

    Terrain terrain = (Terrain)EntityManager.GetGlobalComponent<Terrain>();
    //do something with terrain.

To add a component to an entity you can use the following example:

    EntityManager.AddComponentToEntity<Type>(int entity, object component);

### Systems:
Systems are manually loaded and processed right now.
Systems classes should be static and may include one or more of the following methods, Load(), Update(), Draw(). Having private or public functions differing from these is okay as some things will need their own systems to interact with each other outside of these.
In the future, automatically loading certain systems based on a priority may be implemented for Load(), Update() and Draw() methods.