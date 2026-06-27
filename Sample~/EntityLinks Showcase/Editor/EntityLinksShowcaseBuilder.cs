using System.Collections.Generic;
using TMPro;
using Unity.Scenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using TargetsAuthoring = BovineLabs.Reaction.Authoring.Core.TargetsAuthoring;
using TargetSlot = BovineLabs.Reaction.Data.Core.Target;
using EntityLinkSchema = BovineLabs.Timeline.EntityLinks.Authoring.EntityLinkSchema;
using EntityLinkRootAuthoring = BovineLabs.Timeline.EntityLinks.Authoring.EntityLinkRootAuthoring;
using EntityLinkSourceAuthoring = BovineLabs.Timeline.EntityLinks.Authoring.EntityLinkSourceAuthoring;
using TimelineBeginAuthoring = BovineLabs.Timeline.Core.Authoring.TimelineBeginAuthoring;
using TimelineBeginMode = BovineLabs.Timeline.Core.Authoring.TimelineBeginMode;
using LifeCycleAuthoring = BovineLabs.Core.Authoring.LifeCycle.LifeCycleAuthoring;
using CopyTrack = BovineLabs.Timeline.EntityLinks.Authoring.EntityLinkCopyTransformTrack;
using CopyClip = BovineLabs.Timeline.EntityLinks.Authoring.EntityLinkCopyTransformClip;
using MutateTrack = BovineLabs.Timeline.EntityLinks.Authoring.EntityLinkMutateTrack;
using MutateClip = BovineLabs.Timeline.EntityLinks.Authoring.EntityLinkMutateClip;
using MutateMode = BovineLabs.Timeline.EntityLinks.Data.EntityLinkMutateMode;
using ParentTrack = BovineLabs.Timeline.EntityLinks.Authoring.EntityLinkParentTrack;
using ParentClip = BovineLabs.Timeline.EntityLinks.Authoring.EntityLinkParentClip;
using PatchTrack = BovineLabs.Timeline.EntityLinks.Authoring.EntityLinkTargetPatchTrack;
using PatchClip = BovineLabs.Timeline.EntityLinks.Authoring.EntityLinkTargetPatchClip;
using PositionTrack = BovineLabs.Timeline.Transform.Authoring.TransformPositionTrack;
using PositionClip = BovineLabs.Timeline.Transform.Authoring.PositionClip;
using PositionType = BovineLabs.Timeline.Transform.Authoring.PositionType;

public static class EntityLinksShowcaseBuilder
{
    private const string SampleFolder = "Assets/Samples/EntityLinksShowcase";
    private const string TimelineFolder = SampleFolder + "/Timelines";
    private const string ParentPath = SampleFolder + "/EntityLinksShowcase.unity";
    private const string SubPath = SampleFolder + "/EntityLinksShowcase_Sub.unity";

    private const string PrimarySchemaPath = "Assets/Training/00-foundations/Schema_Actor.asset";
    private const string SwapSchemaPath = "Assets/Settings/Schemas/EntityLinks/Movement Body Link.asset";

    private static readonly Color CopyColor = new Color(0.85f, 0.20f, 0.40f);
    private static readonly Color MutateColor = new Color(0.85f, 0.55f, 0.20f);
    private static readonly Color ParentColor = new Color(0.80f, 0.30f, 0.85f);
    private static readonly Color PatchColor = new Color(0.25f, 0.80f, 0.80f);
    private static readonly Color LeaderColor = new Color(0.95f, 0.20f, 0.20f);
    private static readonly Color FollowerColor = new Color(0.30f, 0.70f, 1.00f);
    private static readonly Color RootColor = new Color(0.45f, 0.47f, 0.52f);
    private static readonly Color AltColor = new Color(0.30f, 0.90f, 0.55f);
    private static readonly Color PadColor = new Color(0.22f, 0.24f, 0.29f);
    private static readonly Color BannerColor = new Color(0.06f, 0.08f, 0.12f);

    private const float CopyX = -18f;
    private const float MutateX = -6f;
    private const float ParentX = 6f;
    private const float PatchX = 18f;
    private const float RowStep = 7f;
    private const float ActorY = 1.0f;

    private static readonly Vector3 CameraPos = new Vector3(0f, 15f, -30f);

    private static Scene activeSub;
    private static EntityLinkSchema primarySchema;
    private static EntityLinkSchema swapSchema;

    private enum BindKind { Targets }

    private sealed class TrackBind
    {
        public string TrackName;
        public string BindActorName;
    }

    private sealed class CellWire
    {
        public string DirectorName;
        public string TimelinePath;
        public List<TrackBind> Binds;
        public bool Loop;
    }

    private static readonly List<CellWire> Wires = new List<CellWire>();

    private sealed class CaptionData
    {
        public string Title;
        public string Usage;
        public Vector3 CellPos;
        public Color Color;
    }

    private static readonly List<CaptionData> Captions = new List<CaptionData>();

    [MenuItem("Showcase/Build EntityLinks")]
    public static void Build()
    {
        Wires.Clear();
        Captions.Clear();

        primarySchema = AssetDatabase.LoadAssetAtPath<EntityLinkSchema>(PrimarySchemaPath);
        swapSchema = AssetDatabase.LoadAssetAtPath<EntityLinkSchema>(SwapSchemaPath);
        if (primarySchema == null || swapSchema == null)
        {
            Debug.LogError("EntityLinksShowcase: schema asset(s) missing. primary=" + (primarySchema != null) +
                           " swap=" + (swapSchema != null));
            return;
        }

        EnsureFolders();
        ResetAssets();

        var parent = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(parent, ParentPath);
        var sub = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(sub);
        activeSub = sub;

        BuildPads();
        BuildCopyColumn();
        BuildMutateColumn();
        BuildParentColumn();
        BuildPatchColumn();

        EditorSceneManager.SaveScene(sub, SubPath);
        EditorSceneManager.SetActiveScene(parent);
        EditorSceneManager.CloseScene(sub, true);

        sub = EditorSceneManager.OpenScene(SubPath, OpenSceneMode.Additive);
        EditorSceneManager.SetActiveScene(sub);
        activeSub = sub;

        foreach (var w in Wires)
        {
            WireCell(w);
        }

        EditorSceneManager.MarkSceneDirty(sub);
        EditorSceneManager.SaveScene(sub);

        EditorSceneManager.SetActiveScene(parent);
        BuildParent();
        EditorSceneManager.SaveScene(parent);

        EditorSceneManager.CloseScene(sub, true);
        EditorSceneManager.OpenScene(ParentPath, OpenSceneMode.Single);

        Debug.Log("EntityLinksShowcase: built grid at " + ParentPath +
                  " | primarySchema=" + primarySchema.Id + " swapSchema=" + swapSchema.Id +
                  " directors=" + Wires.Count);
    }

    // ============================================================
    //  Trio scaffolding: the moving LEADER is itself the EntityLink
    //  ROOT *and* the primary link SOURCE (single unparented GameObject
    //  so its world-space orbit clip resolves cleanly). OnValidate's
    //  GetComponentsInChildren finds the leader's own source -> Links
    //  = [leader] -> buffer {key10 -> leader}. The ACTOR carries
    //  TargetsAuthoring + an EMPTY-schema source whose Root = the
    //  leader's root, so readRootFrom=Self hops Actor -> Root -> buffer.
    //  For Swap, an AltLeader child (still, not orbited) supplies the
    //  second key under the same root.
    // ============================================================

    private sealed class Trio
    {
        public GameObject Root;
        public GameObject Leader;
        public GameObject Actor;
    }

    private static Trio BuildTrio(string cell, float x, float z, Color actorColor, bool secondLink)
    {
        var leaderHome = new Vector3(x - 1.6f, ActorY + 1.6f, z);
        var leader = MakeSphere(cell + "_Leader", leaderHome, 0.55f, LeaderColor);
        leader.AddComponent<LifeCycleAuthoring>();
        var rootAuth = leader.AddComponent<EntityLinkRootAuthoring>();
        var leaderSrc = leader.AddComponent<EntityLinkSourceAuthoring>();
        leaderSrc.Root = rootAuth;
        leaderSrc.Schemas = new[] { primarySchema };

        if (secondLink)
        {
            var alt = MakeSphere(cell + "_AltLeader", new Vector3(x + 2.4f, ActorY + 2.4f, z), 0.45f, AltColor);
            alt.transform.SetParent(leader.transform, true);
            alt.AddComponent<LifeCycleAuthoring>();
            var altSrc = alt.AddComponent<EntityLinkSourceAuthoring>();
            altSrc.Root = rootAuth;
            altSrc.Schemas = new[] { swapSchema };
            rootAuth.Links = new[] { leaderSrc, altSrc };
        }
        else
        {
            rootAuth.Links = new[] { leaderSrc };
        }

        var actor = MakeCube(cell + "_Actor", new Vector3(x + 1.6f, ActorY, z), new Vector3(0.9f, 0.9f, 0.9f), actorColor);
        actor.AddComponent<LifeCycleAuthoring>();
        var targets = actor.AddComponent<TargetsAuthoring>();
        targets.Owner = actor;
        targets.Source = actor;
        targets.Target = actor;
        targets.Custom = actor;
        var actorSrc = actor.AddComponent<EntityLinkSourceAuthoring>();
        actorSrc.Root = rootAuth;
        actorSrc.Schemas = System.Array.Empty<EntityLinkSchema>();

        DriveLeaderOrbit(cell, leader, leaderHome);

        return new Trio { Root = leader, Leader = leader, Actor = actor };
    }

    private static void DriveLeaderOrbit(string cell, GameObject leader, Vector3 home)
    {
        var path = TimelineFolder + "/" + cell + "_LeaderOrbit.playable";
        var timeline = NewTimeline(path);
        var track = timeline.CreateTrack<PositionTrack>(null, "Position");
        track.ResetPositionOnDeactivate = true;
        var a = AddWorldPos(track, 0.0, 1.6, "right", home + new Vector3(2.4f, 0.8f, 0.6f));
        var b = AddWorldPos(track, 1.6, 1.6, "left", home + new Vector3(-2.4f, -0.4f, 0.6f));
        var c = AddWorldPos(track, 3.2, 1.6, "fwd", home + new Vector3(0f, 0.6f, 2.0f));
        var d = AddWorldPos(track, 4.8, 1.6, "home", home);
        Blend(a, b, c, d);
        FixDuration(timeline);
        Dirty(timeline, track);
        AssetDatabase.SaveAssets();

        var dirName = cell + "_LeaderDir";
        MakeDirector(dirName, true);
        Wires.Add(new CellWire
        {
            DirectorName = dirName,
            TimelinePath = path,
            Loop = true,
            Binds = new List<TrackBind> { new TrackBind { TrackName = "Position", BindActorName = leader.name } },
        });
    }

    private static TimelineClip AddWorldPos(PositionTrack t, double start, double dur, string name, Vector3 world)
    {
        var c = AddClip<PositionClip>(t, start, dur, name);
        var a = (PositionClip)c.asset;
        a.Type = PositionType.World;
        a.Position = world;
        Dirty(c.asset);
        return c;
    }

    // ============================================================
    //  COPY TRANSFORM
    // ============================================================

    private static void BuildCopyColumn()
    {
        // Row 0 — hover follow (position only, +Y offset).
        {
            var z = 0 * RowStep;
            var cell = "Copy0";
            var trio = BuildTrio(cell, CopyX, z, FollowerColor, false);

            var timeline = NewTimeline(TimelineFolder + "/" + cell + ".playable");
            var t = timeline.CreateTrack<CopyTrack>(null, "Copy");
            var clip = AddClip<CopyClip>(t, 0.0, 6.0, "hover follow +Y");
            var ca = (CopyClip)clip.asset;
            ca.entityToMove = TargetSlot.Self;
            ca.readRootFrom = TargetSlot.Self;
            ca.link = primarySchema;
            ca.copyPosition = true;
            ca.copyRotation = false;
            ca.positionOffset = new Vector3(0f, -1.6f, 0f);
            ca.rotationOffset = Vector3.zero;
            Dirty(clip.asset);

            FinishCell(timeline, cell, CopyX, z, false,
                "Hover-follow (pos only)",
                "EntityLinkCopyTransformClip copies the linked red leader's WORLD position each frame (rotation off) with a -Y offset -> blue follower tracks the orbiting leader from below (loops).",
                CopyColor, "Copy", cell + "_Actor");
        }

        // Row 1 — full snap (position + rotation).
        {
            var z = 1 * RowStep;
            var cell = "Copy1";
            var trio = BuildTrio(cell, CopyX, z, FollowerColor, false);

            var timeline = NewTimeline(TimelineFolder + "/" + cell + ".playable");
            var t = timeline.CreateTrack<CopyTrack>(null, "Copy");
            var clip = AddClip<CopyClip>(t, 0.0, 6.0, "full snap");
            var ca = (CopyClip)clip.asset;
            ca.entityToMove = TargetSlot.Self;
            ca.readRootFrom = TargetSlot.Self;
            ca.link = primarySchema;
            ca.copyPosition = true;
            ca.copyRotation = true;
            ca.positionOffset = new Vector3(0f, -1.6f, 0f);
            ca.rotationOffset = new Vector3(0f, 90f, 0f);
            Dirty(clip.asset);

            FinishCell(timeline, cell, CopyX, z, false,
                "Full snap (pos + rot)",
                "Same follow but copyRotation=true with a 90 deg yaw offset -> follower matches the leader's pose, snapping onto it each frame (loops).",
                CopyColor, "Copy", cell + "_Actor");
        }
    }

    // ============================================================
    //  LINK MUTATE  (Assign / Swap / Remove) — each followed by a
    //  CopyTransform companion so the mutation's effect is visible
    //  as whether/where the follower tracks.
    // ============================================================

    private static void BuildMutateColumn()
    {
        MutateCell(0, "Assign", MutateMode.Assign,
            "EntityLinkMutateClip Mode=Assign overwrites/append key->Target at clip start (edge, PERMANENT). A CopyTransform companion then follows the linked leader -> proves the assigned link resolves (loops).");

        MutateCell(1, "Swap", MutateMode.Swap,
            "Mode=Swap exchanges the entities under two link keys (primary <-> Movement Body) once at start. The follower flips which leader it tracks. Swap is self-inverse (loops re-swap).");

        MutateCell(2, "Remove", MutateMode.Remove,
            "Mode=Remove deletes ALL entries for the key once at start (PERMANENT). The CopyTransform companion then resolves nothing -> follower stops tracking (stays put) -> the silent runtime skip (loops).");
    }

    private static void MutateCell(int row, string label, MutateMode mode, string usage)
    {
        var z = row * RowStep;
        var cell = "Mut" + row;
        var secondLink = mode == MutateMode.Swap;
        var trio = BuildTrio(cell, MutateX, z, MutateColor, secondLink);

        var timeline = NewTimeline(TimelineFolder + "/" + cell + ".playable");

        var mt = timeline.CreateTrack<MutateTrack>(null, "Mutate");
        var mclip = AddClip<MutateClip>(mt, 0.0, 0.5, label);
        var ma = (MutateClip)mclip.asset;
        ma.mode = mode;
        ma.link = primarySchema;
        ma.readRootFrom = TargetSlot.Self;
        ma.newTarget = TargetSlot.Self;
        ma.swapLink = secondLink ? swapSchema : null;
        Dirty(mclip.asset);

        // Companion CopyTransform reveals the mutated map: it follows whatever
        // primarySchema resolves to AFTER the mutation fires.
        var ct = timeline.CreateTrack<CopyTrack>(null, "Copy");
        var cclip = AddClip<CopyClip>(ct, 0.5, 5.5, "follow linked");
        var cc = (CopyClip)cclip.asset;
        cc.entityToMove = TargetSlot.Self;
        cc.readRootFrom = TargetSlot.Self;
        cc.link = primarySchema;
        cc.copyPosition = true;
        cc.copyRotation = false;
        cc.positionOffset = new Vector3(0f, -1.6f, 0f);
        Dirty(cclip.asset);

        var wire = NewWire(timeline, cell + "_Director", false);
        wire.Binds.Add(new TrackBind { TrackName = "Mutate", BindActorName = cell + "_Actor" });
        wire.Binds.Add(new TrackBind { TrackName = "Copy", BindActorName = cell + "_Actor" });
        FinishWire(timeline, wire, MutateX, z, label, usage, MutateColor);
    }

    // ============================================================
    //  PARENT  (restoreOnEnd true vs false)
    // ============================================================

    private static void BuildParentColumn()
    {
        ParentCell(0, "Stick & release", true,
            "EntityLinkParentClip reparents the follower under the linked leader (local pos (0,2,0), yaw 45) while active; restoreOnEnd=true reverts the parent + captured pose when the clip ends -> follower rides the leader, then snaps home each loop.");

        ParentCell(1, "Stick forever", false,
            "Same reparent but restoreOnEnd=false -> the parent change is PERMANENT (Mutate-style). The follower stays attached to the orbiting leader and rides it continuously (loops).");
    }

    private static void ParentCell(int row, string label, bool restoreOnEnd, string usage)
    {
        var z = row * RowStep;
        var cell = "Par" + row;
        var trio = BuildTrio(cell, ParentX, z, ParentColor, false);

        var timeline = NewTimeline(TimelineFolder + "/" + cell + ".playable");
        var t = timeline.CreateTrack<ParentTrack>(null, "Parent");
        // Clip occupies only part of the loop so the restoreOnEnd revert is visible.
        var clip = AddClip<ParentClip>(t, 0.0, 4.0, restoreOnEnd ? "stick (revert)" : "stick (keep)");
        var pa = (ParentClip)clip.asset;
        pa.entityToParent = TargetSlot.Self;
        pa.readRootFrom = TargetSlot.Self;
        pa.parentLink = primarySchema;
        pa.localPosition = new Vector3(0f, 2.0f, 0f);
        pa.localRotation = new Vector3(0f, 45f, 0f);
        pa.restoreOnEnd = restoreOnEnd;
        Dirty(clip.asset);

        var wire = NewWire(timeline, cell + "_Director", false);
        wire.Binds.Add(new TrackBind { TrackName = "Parent", BindActorName = cell + "_Actor" });
        FinishWire(timeline, wire, ParentX, z, label, usage, ParentColor);
    }

    // ============================================================
    //  TARGET PATCH  (write a resolved entity into a Targets slot)
    // ============================================================

    private static void BuildPatchColumn()
    {
        // Row 0 — patch Custom then follow Custom.
        {
            var z = 0 * RowStep;
            var cell = "Pat0";
            var trio = BuildTrio(cell, PatchX, z, PatchColor, false);

            var timeline = NewTimeline(TimelineFolder + "/" + cell + ".playable");
            var pt = timeline.CreateTrack<PatchTrack>(null, "Patch");
            var clip = AddClip<PatchClip>(pt, 0.0, 0.5, "patch Custom<-link");
            var pa = (PatchClip)clip.asset;
            pa.Link = primarySchema;
            pa.ReadRootFrom = TargetSlot.Self;
            pa.WriteTo = TargetSlot.Custom;
            pa.Fallback = TargetSlot.None;
            Dirty(clip.asset);

            // Companion CopyTransform reads the freshly-patched Custom slot as its
            // source root -> follows the leader only because the patch succeeded.
            var ct = timeline.CreateTrack<CopyTrack>(null, "Copy");
            var cclip = AddClip<CopyClip>(ct, 0.5, 5.5, "follow via Custom");
            var cc = (CopyClip)cclip.asset;
            cc.entityToMove = TargetSlot.Self;
            cc.readRootFrom = TargetSlot.Self;
            cc.link = primarySchema;
            cc.copyPosition = true;
            cc.copyRotation = false;
            cc.positionOffset = new Vector3(0f, -1.6f, 0f);
            Dirty(cclip.asset);

            var wire = NewWire(timeline, cell + "_Director", false);
            wire.Binds.Add(new TrackBind { TrackName = "Patch", BindActorName = cell + "_Actor" });
            wire.Binds.Add(new TrackBind { TrackName = "Copy", BindActorName = cell + "_Actor" });
            FinishWire(timeline, wire, PatchX, z, label: "Patch Custom slot",
                usage: "EntityLinkTargetPatchClip writes the linked leader entity into the Custom Targets slot once at start (PERMANENT, skip-on-Null). The follower then tracks the leader, proving the patch resolved (loops).",
                color: PatchColor);
        }

        // Row 1 — patch Target with a fallback to Self (resolves to leader, so fallback unused).
        {
            var z = 1 * RowStep;
            var cell = "Pat1";
            var trio = BuildTrio(cell, PatchX, z, PatchColor, false);

            var timeline = NewTimeline(TimelineFolder + "/" + cell + ".playable");
            var pt = timeline.CreateTrack<PatchTrack>(null, "Patch");
            var clip = AddClip<PatchClip>(pt, 0.0, 0.5, "patch Target<-link (fb Self)");
            var pa = (PatchClip)clip.asset;
            pa.Link = primarySchema;
            pa.ReadRootFrom = TargetSlot.Self;
            pa.WriteTo = TargetSlot.Target;
            pa.Fallback = TargetSlot.Self;
            Dirty(clip.asset);

            var ct = timeline.CreateTrack<CopyTrack>(null, "Copy");
            var cclip = AddClip<CopyClip>(ct, 0.5, 5.5, "follow via Target");
            var cc = (CopyClip)cclip.asset;
            cc.entityToMove = TargetSlot.Self;
            cc.readRootFrom = TargetSlot.Self;
            cc.link = primarySchema;
            cc.copyPosition = true;
            cc.copyRotation = false;
            cc.positionOffset = new Vector3(0f, -1.6f, 0f);
            Dirty(cclip.asset);

            var wire = NewWire(timeline, cell + "_Director", false);
            wire.Binds.Add(new TrackBind { TrackName = "Patch", BindActorName = cell + "_Actor" });
            wire.Binds.Add(new TrackBind { TrackName = "Copy", BindActorName = cell + "_Actor" });
            FinishWire(timeline, wire, PatchX, z, label: "Patch Target (+ fallback)",
                usage: "Same one-shot patch but WriteTo=Target with Fallback=Self. The link resolves to the leader so the fallback is unused; follower tracks the leader (loops).",
                color: PatchColor);
        }
    }

    // ============================================================
    //  wire / caption plumbing
    // ============================================================

    private static void FinishCell(TimelineAsset timeline, string cell, float x, float z, bool loop,
        string label, string usage, Color color, string trackName, string actorName)
    {
        var wire = NewWire(timeline, cell + "_Director", loop);
        wire.Binds.Add(new TrackBind { TrackName = trackName, BindActorName = actorName });
        FinishWire(timeline, wire, x, z, label, usage, color);
    }

    private static CellWire NewWire(TimelineAsset timeline, string directorName, bool loop)
    {
        return new CellWire
        {
            DirectorName = directorName,
            TimelinePath = AssetDatabase.GetAssetPath(timeline),
            Loop = loop,
            Binds = new List<TrackBind>(),
        };
    }

    private static void FinishWire(TimelineAsset timeline, CellWire wire, float x, float z, string label, string usage, Color color)
    {
        FixDuration(timeline);
        Dirty(timeline);
        foreach (var tr in timeline.GetOutputTracks()) Dirty(tr);
        AssetDatabase.SaveAssets();
        MakeDirector(wire.DirectorName, wire.Loop);
        Wires.Add(wire);
        Captions.Add(new CaptionData { Title = label, Usage = usage, CellPos = new Vector3(x, 3.8f, z), Color = color });
    }

    private static void WireCell(CellWire w)
    {
        var director = GameObject.Find(w.DirectorName).GetComponent<PlayableDirector>();
        var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(w.TimelinePath);
        director.playableAsset = timeline;

        foreach (var track in timeline.GetOutputTracks())
        {
            var bind = FindBind(w, track.name);
            if (bind == null) continue;
            var actor = GameObject.Find(bind.BindActorName);
            if (track.name == "Position")
            {
                director.SetGenericBinding(track, actor.transform);
            }
            else
            {
                director.SetGenericBinding(track, actor.GetComponent<TargetsAuthoring>());
            }
        }

        EditorUtility.SetDirty(director);
    }

    private static TrackBind FindBind(CellWire w, string trackName)
    {
        foreach (var b in w.Binds)
            if (b.TrackName == trackName)
                return b;
        return null;
    }

    private static PlayableDirector MakeDirector(string name, bool loop)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, activeSub);
        var director = go.AddComponent<PlayableDirector>();
        director.playOnAwake = true;
        director.extrapolationMode = DirectorWrapMode.Loop;
        var begin = go.AddComponent<TimelineBeginAuthoring>();
        begin.Mode = TimelineBeginMode.OnLoad;
        begin.DelaySeconds = 0f;
        return director;
    }

    private static TimelineAsset NewTimeline(string path)
    {
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, path);
        return timeline;
    }

    private static TimelineClip AddClip<T>(TrackAsset track, double start, double duration, string name) where T : PlayableAsset
    {
        var clip = track.CreateClip<T>();
        clip.start = start;
        clip.duration = duration;
        clip.displayName = name;
        return clip;
    }

    private static void Blend(params TimelineClip[] clips)
    {
        foreach (var c in clips) c.blendInDuration = 0.4;
    }

    private static void FixDuration(TimelineAsset timeline)
    {
        var end = 0.0;
        foreach (var track in timeline.GetOutputTracks())
            foreach (var clip in track.GetClips())
            {
                var clipEnd = clip.start + clip.duration;
                if (clipEnd > end) end = clipEnd;
            }

        timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
        timeline.fixedDuration = end;
    }

    // ============================================================
    //  primitives
    // ============================================================

    private static GameObject MakeCube(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = MakeMaterial(name, color);
        SceneManager.MoveGameObjectToScene(go, activeSub);
        return go;
    }

    private static GameObject MakeSphere(string name, Vector3 pos, float radius, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = new Vector3(radius * 2f, radius * 2f, radius * 2f);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = MakeMaterial(name, color);
        SceneManager.MoveGameObjectToScene(go, activeSub);
        return go;
    }

    private static GameObject MakePad(string name, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = size;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = MakeMaterial(name, PadColor);
        SceneManager.MoveGameObjectToScene(go, activeSub);
        return go;
    }

    private static void BuildPads()
    {
        float[] xs = { CopyX, MutateX, ParentX, PatchX };
        string[] names = { "Copy", "Mutate", "Parent", "Patch" };
        for (var i = 0; i < xs.Length; i++)
            MakePad(names[i] + "_Pad", new Vector3(xs[i], 0.05f, 4f), new Vector3(9.0f, 0.12f, 22f));
    }

    private static Material MakeMaterial(string name, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader) { name = name + "_Mat" };
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        return mat;
    }

    // ============================================================
    //  parent scene: camera, labels, subscene
    // ============================================================

    private static void BuildParent()
    {
        FrameCamera();
        RenderSettings.fog = false;

        MakeBanner("Title_Banner", new Vector3(0f, 13.6f, 0f), new Vector3(48f, 3.4f, 0.1f));
        MakeWorldLabel("Title", "ENTITY LINKS TIMELINE GRID", new Vector3(0f, 14.0f, -0.4f), 48f, Color.white, 5.0f, TextAlignmentOptions.Center);
        MakeWorldLabel("Subtitle", "4 tracks · 4 clip types resolving links by schema   ·   com.bovinelabs.timeline.entitylinks", new Vector3(0f, 12.7f, -0.4f), 48f, new Color(0.85f, 0.9f, 1f), 1.9f, TextAlignmentOptions.Center);

        MakeColumnHeader("Copy_Header", "COPY TRANSFORM", CopyX, CopyColor);
        MakeColumnHeader("Mutate_Header", "LINK MUTATE", MutateX, MutateColor);
        MakeColumnHeader("Parent_Header", "PARENT", ParentX, ParentColor);
        MakeColumnHeader("Patch_Header", "TARGET PATCH", PatchX, PatchColor);

        foreach (var cap in Captions)
            MakeCaption(cap.Title, cap.Usage, cap.CellPos, cap.Color);

        MakeBanner("Usage_Banner", new Vector3(0f, 0.7f, -7.5f), new Vector3(54f, 2.0f, 0.1f));
        MakeWorldLabel("Usage",
            "Red spheres = orbiting LEADERS (the link source, key 10). Blue/coloured cubes = ACTORS bound to each director (readRootFrom=Self). Every clip resolves the leader via the EntityLink schema; CopyTransform companions make Mutate/Patch effects visible as following. FixedLength + Loop.",
            new Vector3(0f, 0.7f, -7.8f), 52f, new Color(0.96f, 0.97f, 1f), 1.5f, TextAlignmentOptions.Center);

        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(SubPath);
        if (sceneAsset == null)
        {
            Debug.LogError("EntityLinksShowcase: sub-scene asset missing at " + SubPath);
            return;
        }

        var subSceneGo = new GameObject("Showcase SubScene");
        var subScene = subSceneGo.AddComponent<SubScene>();
        subScene.SceneAsset = sceneAsset;
        subScene.AutoLoadScene = true;
        EditorUtility.SetDirty(subScene);
    }

    private static void MakeColumnHeader(string name, string text, float x, Color color)
    {
        var pos = new Vector3(x, 4.0f, -4.5f);
        MakeBanner(name + "_Banner", pos + new Vector3(0f, 0f, 0.08f), new Vector3(8.4f, 1.3f, 0.1f));
        MakeWorldLabel(name, "<b>" + text + "</b>", pos, 8.2f, color, 2.6f, TextAlignmentOptions.Center);
    }

    private static float CaptionY(float z)
    {
        return 4.4f + z * 0.16f;
    }

    private static void MakeCaption(string title, string usage, Vector3 cellPos, Color color)
    {
        var z = cellPos.z;
        var y = CaptionY(z);
        MakeBanner("CapBanner_" + title + "_" + z, new Vector3(cellPos.x, y, z + 0.06f), new Vector3(8.4f, 2.0f, 0.05f));
        MakeWorldLabel("Cap_" + title + "_" + z, "<b>" + title + "</b>", new Vector3(cellPos.x, y + 0.5f, z), 8.2f, color, 2.4f, TextAlignmentOptions.Center);
        MakeWorldLabel("Use_" + title + "_" + z, usage, new Vector3(cellPos.x, y - 0.42f, z), 8.2f, new Color(0.95f, 0.96f, 1f), 1.2f, TextAlignmentOptions.Center);
    }

    private static void FrameCamera()
    {
        var required = GameObject.Find("Required In Scene");
        if (required == null) return;
        var camTransform = required.transform.Find("Main Camera");
        if (camTransform == null) return;
        camTransform.position = CameraPos;
        camTransform.rotation = Quaternion.Euler(20f, 0f, 0f);
        var cam = camTransform.GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = 60f;
            cam.farClipPlane = 400f;
            EditorUtility.SetDirty(cam);
        }

        EditorUtility.SetDirty(camTransform);
    }

    private static void MakeBanner(string name, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(pos - CameraPos, Vector3.up);
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = MakeMaterial(name, BannerColor);
    }

    private static void MakeWorldLabel(string name, string text, Vector3 pos, float width, Color color, float fontSize, TextAlignmentOptions alignment)
    {
        var holder = new GameObject(name);
        holder.transform.position = pos;
        holder.transform.rotation = Quaternion.LookRotation(pos - CameraPos, Vector3.up);

        var go = new GameObject("Text");
        go.transform.SetParent(holder.transform, false);
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.rectTransform.sizeDelta = new Vector2(width, 4f);
        tmp.rectTransform.localPosition = Vector3.zero;
        tmp.fontStyle = FontStyles.Bold;
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Samples"))
            AssetDatabase.CreateFolder("Assets", "Samples");
        if (!AssetDatabase.IsValidFolder(SampleFolder))
            AssetDatabase.CreateFolder("Assets/Samples", "EntityLinksShowcase");
        if (!AssetDatabase.IsValidFolder(TimelineFolder))
            AssetDatabase.CreateFolder(SampleFolder, "Timelines");
    }

    private static void ResetAssets()
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(TimelineFolder) != null)
            foreach (var guid in AssetDatabase.FindAssets("t:TimelineAsset", new[] { TimelineFolder }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

        foreach (var p in new[] { ParentPath, SubPath })
            if (AssetDatabase.LoadAssetAtPath<Object>(p) != null)
                AssetDatabase.DeleteAsset(p);
    }

    private static void Dirty(params Object[] objects)
    {
        foreach (var o in objects)
            EditorUtility.SetDirty(o);
    }
}
