using Content.Stellar.Shared.CosmicCult.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Stellar.Server.CosmicCult;

public sealed partial class CleanseCult : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-cleanse-cultist", ("chance", Probability));
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.TargetEntity;
        if (entityManager.HasComponent<CosmicCultComponent>(uid))
            entityManager.EnsureComponent<CleanseCultComponent>(uid); // We just slap them with the component and let the Deconversion system handle the rest.
    }
}
