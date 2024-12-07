using System.Collections.Generic;

//wrapper class for cullers, checks all filters at once
public class CullingStack
{
    public List<Culling> stack;
    public List<PlayerEntity> group;

    public CullingStack()
    {
        stack = new List<Culling>();
        group = new List<PlayerEntity>();
    }

    public bool ApplyCulling(Entity entity)
    {
        int stackCount = stack.Count;

        bool hadRequirement = false;

        for (int i = 0; i < stackCount; i++)
        {
            Culling culling = stack[i];

            bool test = culling.ApplyCulling(entity, group);

            if (culling.mode == ECullingMode.REQUIREMENT)
            {
                hadRequirement = true;

                if (!test)
                {
                    return false;
                }
            }

            if (test && culling.mode == ECullingMode.OPTIONAL)
            {
                return true;
            }
        }

        return hadRequirement;
    }
}
