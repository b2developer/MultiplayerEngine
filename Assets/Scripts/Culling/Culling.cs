using System.Collections.Generic;

public enum ECullingMode
{
    REQUIREMENT, //culling filter must be passed
    OPTIONAL, //culling filter can result in a pass
}

public class Culling
{
    public ECullingMode mode;

    public virtual bool ApplyCulling(Entity entity, List<PlayerEntity> group)
    {
        return true;
    }

    public virtual Culling Clone()
    {
        return new Culling();
    }
}
