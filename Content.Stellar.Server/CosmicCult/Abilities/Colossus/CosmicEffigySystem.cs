using System.Numerics;
using Content.Server.Actions;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Popups;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Warps;
using Content.Stellar.Server.CosmicCult.Components;
using Content.Stellar.Shared.CosmicCult.Components;
using Content.Stellar.Shared.CosmicCult;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;

namespace Content.Stellar.Server.CosmicCult.Abilities;

public sealed class CosmicEffigySystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly CodeConditionSystem _codeCondition = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CosmicColossusComponent, EventCosmicColossusEffigy>(OnColossusEffigy);
    }

    private void OnColossusEffigy(Entity<CosmicColossusComponent> ent, ref EventCosmicColossusEffigy args)
    {
        if (!VerifyPlacement(ent, out var pos))
            return;

        _actions.RemoveAction(ent.Owner, ent.Comp.EffigyPlaceActionEntity);
        _codeCondition.SetCompleted(ent.Owner, ent.Comp.EffigyObjective);
        Spawn(ent.Comp.EffigyPrototype, pos);
        ent.Comp.Timed = false;
    }

    private bool VerifyPlacement(Entity<CosmicColossusComponent> ent, out EntityCoordinates outPos)
    {
        // MAKE SURE WE'RE STANDING ON A GRID
        var xform = Transform(ent);
        outPos = new EntityCoordinates();

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            _popup.PopupEntity(Loc.GetString("ghost-role-colossus-effigy-error-grid"), ent, ent);
            return false;
        }

        var localTile = _map.GetTileRef(xform.GridUid.Value, grid, xform.Coordinates);
        var targetIndices = localTile.GridIndices + new Vector2i(0, 2);
        var pos = _map.ToCenterCoordinates(xform.GridUid.Value, targetIndices, grid);
        outPos = pos;
        var box = new Box2(pos.Position + new Vector2(-1.4f, -0.4f), pos.Position + new Vector2(1.4f, 0.4f));

        // CHECK IF IT'S BEING PLACED CHEESILY CLOSE TO SPACE
        var spaceDistance = 2;
        var worldPos = _transform.GetWorldPosition(xform);
        foreach (var tile in _map.GetTilesIntersecting(xform.GridUid.Value, grid, new Circle(worldPos, spaceDistance)))
        {
            if (_turf.IsSpace(tile))
            {
                _popup.PopupEntity(Loc.GetString("ghost-role-colossus-effigy-error-space", ("DISTANCE", spaceDistance)), ent, ent);
                return false;
            }
        }

        // CHECK FOR ENTITY AND ENVIRONMENTAL INTERSECTIONS
        if (_lookup.AnyLocalEntitiesIntersecting(xform.GridUid.Value, box, LookupFlags.Dynamic | LookupFlags.Static, ent))
        {
            _popup.PopupEntity(Loc.GetString("ghost-role-colossus-effigy-error-intersection"), ent, ent);
            return false;
        }

        // IF THE OBJECTIVE OR LOCATION IS MISSING, PLACE IT ANYWHERE
        if (!_mind.TryGetObjectiveComp<CosmicEffigyConditionComponent>(ent, out var obj) || obj.EffigyTarget == null)
            return true;

        var targetXform = Transform(obj.EffigyTarget.Value);
        if (xform.MapID != targetXform.MapID || (_transform.GetWorldPosition(xform) - _transform.GetWorldPosition(targetXform)).LengthSquared() > 15 * 15)
        {
            if (TryComp<WarpPointComponent>(obj.EffigyTarget, out var warp) && warp.Location is not null)
                _popup.PopupEntity(Loc.GetString("ghost-role-colossus-effigy-error-location", ("LOCATION", warp.Location)), ent, ent);
            return false;
        }

        return true;
    }
}
