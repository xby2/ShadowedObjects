# ShadowedObjects #

ShadowedObjects is a library that adds object-level undo capabilities to
your custom business objects.

## Examples ##

### Simple Properties
```csharp
[Shadowed]
public class MyBusinessObject
{
    [Shadowed]
    public virtual string Name { get; set; }
}

var myObj = ShadowedObject.Create<MyBusinessObject>();
myObj.Name = "Pinky";
myObj.BaselineOriginals();
myObj.Name = "Brain";
myObj.ResetToOriginal(o => o.Name);
Assert.IsTrue(myObj.Name == "Pinky"); //true
```
