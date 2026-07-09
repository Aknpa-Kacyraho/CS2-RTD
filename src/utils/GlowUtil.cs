using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace RollTheDice.Utils
{
    public static class GlowUtil
    {
        public static (CDynamicProp?, CDynamicProp?) CreateGlow(CBaseEntity entity, Color color)
        {
            CDynamicProp? _glowProxy = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            CDynamicProp? _glow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (_glowProxy == null || _glow == null)
                return (null, null);

            var skeleton = entity.CBodyComponent?.SceneNode?.GetSkeletonInstance();
            if (skeleton == null)
                return (null, null);
            string modelName = skeleton.ModelState.ModelName;
            if (string.IsNullOrEmpty(modelName))
                return (null, null);

            _glowProxy.Spawnflags = 256u;
            _glowProxy.RenderMode = RenderMode_t.kRenderNone;
            _glowProxy.SetModel(modelName);
            _glowProxy.AcceptInput("FollowEntity", entity, _glowProxy, "!activator");
            _glowProxy.DispatchSpawn();

            _glow.SetModel(modelName);
            _glow.AcceptInput("FollowEntity", _glowProxy, _glow, "!activator");
            _glow.DispatchSpawn();
            _glow.Render = Color.FromArgb(255, 255, 255, 255);
            _glow.Glow.GlowColorOverride = color;
            _glow.Spawnflags = 256u;
            _glow.RenderMode = RenderMode_t.kRenderNormal;
            _glow.Glow.GlowRange = 5000;
            _glow.Glow.GlowTeam = -1;
            _glow.Glow.GlowType = 3;
            _glow.Glow.GlowRangeMin = 20;
            return (_glowProxy, _glow);
        }

        public static void RemoveGlow(CBaseEntity? glowProxy, CBaseEntity? glow)
        {
            Entities.RemoveEntity(glowProxy);
            Entities.RemoveEntity(glow);
        }
    }
}
