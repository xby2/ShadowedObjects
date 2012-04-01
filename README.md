ShadowedObjects is a library that provides object-level undo capabilities to
your custom business objects.

# Examples #

```
[Shadowed]
public class TestLevelA
{
    [Shadowed]
    public virtual string Name { get; set; }
}

var myObj = ShadowedObject.Create<MyBusinessObject>();
myObj.Name = "Pinky";
myObj.BaselineOriginals();
myObj.Name = "Brain";
myObj.ResetToOriginal(o => o.Name);
// myObj.name == "Pinky"
```
