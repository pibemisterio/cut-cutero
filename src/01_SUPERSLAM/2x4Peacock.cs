using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
namespace MMXOnline;

#region ▄▄★ PEACOCK ★▄▄ 

public class X4Peacock : Maverick {
    public static Weapon getWeapon() { return new Weapon(WeaponIds.FakeZeroGeneric, 150); }
    public HomingCursor? cursor;

    // Main creation function.
    public X4Peacock(
        Player player, Point pos, Point destPos, int xDir,
        ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
    ) : base(
        player, pos, destPos, xDir, netId, ownedByLocalPlayer
    ) {
        stateCooldowns = new() {
            { typeof(FakeZeroMeleeState), new(30) },
            { typeof(FakeZeroGroundPunchState), new(60) },
            { typeof(FakeZeroShootState), new(30, true) },
        };

        weapon = getWeapon();
        //	awardWeaponId = WeaponIds.Buster;
        //	weakWeaponId = WeaponIds.SpeedBurner;
        //	weakMaverickWeaponId = WeaponIds.FlameStag;
        //	canClimbWall = true;
        //	canClimb = true;

        netActorCreateId = NetActorCreateId.FakeZero;
        netOwner = player;
        if (sendRpc) {
            createActorRpc(player.id);
        }

        usesAmmo = true;
        canHealAmmo = true;
        ammo = 28;
        maxAmmo = 28;
        grayAmmoLevel = 3;
        barIndexes = (60, 49);
        //	gameMavs = GameMavs.X2;
        height = 36;
        gravityModifier = 0.8f;
    }

    #region ★ Update ━━━━━
    public override void update() {
        base.update();
        if (!ownedByLocalPlayer) return;

        if (cursor != null && cursor.destroyed) {
            cursor = null;
        }
    }
    #endregion

    #region ★ Atk Ctrl ━━━━━
    public override bool attackCtrl() {
        if (input.isPressed(Control.Jump, player)) {
            if (state is MJump || state is MFall) {
                changeState(new PeacockHover());
            }
        }
        if (input.isPressed(Control.Shoot, player)) {
            if (cursor != null) {
                if (cursor.targetLocked != null) {
                    changeState(new PeacockFireFeather(cursor.targetLocked, fromLoop: false, startedGrounded: this.grounded));
                    return false; //prevent canceling own feather state with feather state
                }
            } else {
                changeState(new PeacockFireCursor(startedGrounded: this.grounded));
            }

        }
        if (input.isPressed(Control.Special1, player)) {
            if (cursor != null) {
                if (cursor.targetLocked != null) {
                    changeState(new PeacockTailJump(cursor.targetLocked));
                    return true;
                }
            } else {
                changeState(new PeacockFireCursor(startedGrounded: this.grounded));
            }
        }
        if (input.isPressed(Control.Dash, player)) {
            if (cursor != null) {
                if (cursor.targetLocked != null) {
                    changeState(new PeacockSwitchStep(cursor.targetLocked));
                    return true;
                }
            } else {
                changeState(new PeacockFireCursor(startedGrounded: this.grounded));
            }

        }
        if (grounded) {
            if (input.isHeld(Control.Down, player) && state is not FakeZeroGuardState) {
                changeState(new FakeZeroGuardState());
                return true;
            }
        }
        return false;
    }
    #endregion

    public override float getRunSpeed() {

        return 100;
    }

    public override string getMaverickPrefix() {
        return "mav_x4pck";
    }

    public override MaverickState[] strikerStates() {
        return [
            new FakeZeroShootState(2),
        ];
    }

    public override MaverickState[] aiAttackStates() {
        List<MaverickState> aiStates = [
            new FakeZeroShootState(1)
        ];

        return aiStates.ToArray();
    }

    public override void aiUpdate() {
        base.aiUpdate();


    }


    public MaverickState getShootState(bool isAI) {
        var mshoot = new MShoot((Point pos, int xDir) => {
            new FakeZeroBusterProj(
                pos, xDir, this, player.getNextActorNetId(), sendRpc: true
            );
        }, "busterX2");
        if (isAI) {
            mshoot.consecutiveData = new MaverickStateConsecutiveData(0, 4, 0.001f);
        }
        return mshoot;
    }

    public override void onDestroy() {
        base.onDestroy();

    }
}


#endregion
#region ▄▄▄■ STATES ■▄▄▄▄




#region ■ Hover ━━━━━
public class PeacockHover : MaverickState {
    public X4Peacock picapau = null!;
    float hoverSpeed = 120;
    float acceleration = 0.2f;
    float deceleration = 0.1f;
    float currentVelocity = 0;

    public PeacockHover() : base("fly") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        picapau = maverick as X4Peacock ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.useGravity = false;
        currentVelocity = 0;
    }

    public override void update() {
        base.update();

        float targetVelocity = 0;


        if (player.input.isHeld(Control.Left, player)) {
            targetVelocity = -hoverSpeed;
        } else if (player.input.isHeld(Control.Right, player)) {
            targetVelocity = hoverSpeed;
        }
        if (Math.Abs(targetVelocity) > 0.1f) {
            currentVelocity = Helpers.lerp(currentVelocity, targetVelocity, acceleration);
        } else {
            currentVelocity = Helpers.lerp(currentVelocity, 0, deceleration);
        }
        if (Math.Abs(currentVelocity) > 0.1f) {
            maverick.move(new Point(currentVelocity, 0));
        }

        if (player.input.isPressed(Control.Jump, player) && stateTime > 0.2f) {
            maverick.changeState(new MFall());
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        maverick.useGravity = true;
    }
}
#endregion



#region ■ State Cursor ━━━
public class PeacockFireCursor : MaverickState {
    public X4Peacock picapau = null!;
    bool hasShot;

    public PeacockFireCursor(bool startedGrounded) : base(startedGrounded ? "1atk_cursor" : "1atk_cursor_air") {
        landSprite = "1atk_feather";
        airSprite = "1atk_feather_air";
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        picapau = maverick as X4Peacock ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.useGravity = false;
    }
    public override void update() {
        base.update();
        if (maverick.frameIndex >= 0 && !hasShot) {
            hasShot = true;
            fireCursor();
        }
        if (maverick.isAnimOver()) {
            maverick.changeToIdleOrFall();
        }
    }
    private void fireCursor() {
        Point? shootPos = maverick.getFirstPOIOrDefault();
        if (shootPos != null) {
            picapau.cursor = new HomingCursor(
                shootPos.Value, maverick.xDir,
                picapau, picapau, player,
                player.getNextActorNetId(), rpc: true
            );
        }
    }
    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        maverick.useGravity = true;
    }
}
#endregion

#region ■ State Feather ━━
public class PeacockFireFeather : MaverickState {
    public X4Peacock picapau = null!;
    Actor? targetFromLock;
    bool hasShot;
    bool fromLoop;
    int flySpeed = 90;


    public PeacockFireFeather(Actor? targetFromLock, bool fromLoop, bool startedGrounded) : base(startedGrounded ? "1atk_feather" : "1atk_feather") {
        this.targetFromLock = targetFromLock;
        this.fromLoop = fromLoop;
        landSprite = "1atk_feather";
        airSprite = "1atk_feather";

    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        picapau = maverick as X4Peacock ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.useGravity = false;
        if (fromLoop) { //basically if holding, redo the state from a better frame
                        //we do not loop inside the same state instance to REASIGN cursor.targetFromLock
            maverick.frameIndex = 4;
        }
    }

    public override void update() {
        base.update();

        movementControl();

        if (maverick.frameIndex >= 9 && !hasShot) {
            hasShot = true;
            fireFeather();
        }
        if (picapau.cursor != null) {
            if (maverick.frameIndex == 12 && player.input.isHeld(Control.Shoot, player) && picapau.cursor.targetLocked != null) {
                maverick.changeState(new PeacockFireFeather(picapau.cursor.targetLocked, fromLoop: true, startedGrounded: maverick.grounded));
            }
        }
        if (maverick.isAnimOver()) {
            maverick.changeToIdleOrFall();
        }
        if (maverick.frameIndex >= 10) {
            attackCtrl = true;
        }
    }
    private void movementControl() {



        if (!maverick.grounded) {
            if (player.input.isHeld(Control.Left, player)) {
                maverick.move(new Point(-flySpeed, 0));
            }
            if (player.input.isHeld(Control.Right, player)) {
                maverick.move(new Point(flySpeed, 0));
            }

        }
    }
    private void fireFeather() {
        Point? shootPos = maverick.getFirstPOIOrDefault();
        if (shootPos != null) {
            new HomingFeather(
                shootPos.Value, maverick.xDir, targetFromLock,
                picapau, player,
                player.getNextActorNetId(), rpc: true
            );
        }
    }
    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        maverick.useGravity = true;
    }
}
#endregion
#region ■ Special Jump ━━━
public class PeacockTailJump : MaverickState {
    public X4Peacock picapau = null!;
    Actor? targetFromLock;
    TailBladeProj? proj;
    bool hasTeleported;
    bool hasCreatedProj;
    float verticalMovement = -500;


    public PeacockTailJump(Actor? targetFromLock) : base("2spc_jump") {
        this.targetFromLock = targetFromLock;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        picapau = maverick as X4Peacock ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.useGravity = false;
    }

    public override void update() {
        base.update();


        if (maverick.frameIndex >= 7 && targetFromLock != null && !hasTeleported) {
            maverick.changePos(targetFromLock.pos);
            hasTeleported = true;
        }
        if (maverick.frameIndex >= 14) {
            maverick.unstickFromGround();
            maverick.move(new Point(0, verticalMovement));
            verticalMovement = Helpers.lerp(verticalMovement, 0, Global.spf * 5.5f);
        }
        if (maverick.frameIndex >= 15 && !hasCreatedProj) {
            hasCreatedProj = true;
            proj = new TailBladeProj(maverick.pos, maverick.xDir, maverick, player, player.getNextActorNetId(), true);
        }
        if (stateTime > 1f) { //not frame index because uuh should be around frame 15 not 16
            attackCtrl = true;
               proj?.destroySelf();
        }
        if (maverick.frameIndex >= 16) {
            proj?.destroySelf();
        }

        if (maverick.isAnimOver()) {
            maverick.changeState(new MFall());

        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        proj?.destroySelf();
        maverick.useGravity = true;
    }
}
#endregion

#region ■ Switch Step ━━━
public class PeacockSwitchStep : MaverickState {
    public X4Peacock picapau = null!;
    Actor? targetFromLock;
    bool hasTeleported;
    bool hasCreatedProj;
    Point picapauStoredPos;


    public PeacockSwitchStep(Actor? targetFromLock) : base("3dash_teleport") {
        this.targetFromLock = targetFromLock;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        picapau = maverick as X4Peacock ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.useGravity = false;
        picapauStoredPos = maverick.pos;
    }

    public override void update() {
        base.update();


        if (maverick.frameIndex >= 5 && targetFromLock != null && !hasTeleported) {
            maverick.changePos(targetFromLock.pos);
            targetFromLock.changePos(picapauStoredPos);
            hasTeleported = true;
        }


        if (maverick.isAnimOver()) {
            maverick.changeToIdleOrFall();

        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        maverick.useGravity = true;
    }
}
#endregion

#endregion

#region ▄▄▄▄⬤ PROJ ⬤▄▄▄▄






#region ⬤ Cursor ━━━━━
public class HomingCursor : Projectile {
    public X4Peacock picapau2;
    public Actor? targetHoming; //two different targets, HOMING, simply causes the cursor to home someone
    public Actor? targetLocked; //LOCKED happens when touching someone, HOMING and LOCKED can be different targets
    public float maxSpeed = 150;
    public HomingCursor(
        Point pos, int xDir, X4Peacock picapau2, Actor owner, Player player, ushort? netId, float? customAngle = null, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4pck_1atk_cursor_proj", netId, player
    ) {
        this.picapau2 = picapau2;
        weapon = FakeZero.getWeapon();
        projId = (int)ProjIds.FakeZeroBuster;
        damager.damage = 0;
        vel = Point.zero;
        maxTime = 1.2f;
        destroyOnHit = false;
        destroyOnHitWall = false;
        fadeSprite = "mav_x4pck_1atk_cursor_proj_fade";
        fadeOnAutoDestroy = true;
        this.customAngle = this.xDir == -1 ? 180 : 0;
        if (customAngle != null) { // "custom"Angle because Projectile angle would rotate the sprite
            this.customAngle = customAngle.Value + (this.xDir == -1 ? 180 : 0);
        }

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new HomingCursor(
            args.pos, args.xDir, null, args.owner, args.player, args.netId //PAPUTODO: nigga pls understand the one from hornet (NULL instead of the id)
        );
    }
    bool homing = true;
    float customAngle = 0;
    public override void update() {
        base.update();
        if (frameIndex <= 10) return;
        vel = new Point(maxSpeed * xDir, 0);
        if (ownedByLocalPlayer && homing) {
            if (targetHoming != null) {
                if (!Global.level.gameObjects.Contains(targetHoming)) {
                    targetHoming = null;
                }
            }
            if (targetHoming != null) {
                maxSpeed = 300;
                frameSpeed = 12;
                if (time < 3f) {
                    var dTo = pos.directionTo(targetHoming.getCenterPos()).normalize();
                    var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
                    destAngle = Helpers.to360(destAngle);
                    customAngle = Helpers.lerpAngle(customAngle, destAngle, Global.spf * 14);
                }
            }
            if (time >= 0.15) {
                targetHoming = Global.level.getClosestTarget(pos, damager.owner.alliance, true, aMaxDist: 300);
            } else if (time < 0.15) {
                //this.vel.x += this.xDir * Global.spf * 300;
            }
            vel.x = Helpers.cosd(customAngle) * maxSpeed;
            vel.y = Helpers.sind(customAngle) * maxSpeed;
        }
    }
    public override void postUpdate() {
        base.postUpdate();
        if (!ownedByLocalPlayer) return;

        if (targetLocked != null) {
            changePos(getTargetPos(targetLocked));

            if (targetLocked.pos.distanceTo(picapau2.pos) > 600 || targetLocked.destroyed || targetLocked is Character chr && chr.charState is Die) {
                targetLocked = null;
                destroySelf();
            }
        }
    }
    public Point getTargetPos([NotNull] Actor targetLocked) { //no idea the hell this does
        if (targetLocked is Character chr) {
            return chr.getParasitePos();
        } else {
            return targetLocked.getCenterPos();
        }
    }

    public override void onHitDamagable(IDamagable damagable) {
        base.onHitDamagable(damagable);
        if (!ownedByLocalPlayer) return;

        if (targetLocked == null) {
            var hitActor = damagable.actor();
            targetLocked = hitActor;
            stopMoving();
            time = 0;
            maxTime = 50;
            forceNetUpdateNextFrame = true;
            playSound("bhornetLockOn", sendRpc: true);
            changeSprite("mav_x4pck_1atk_cursor_lock_proj", true);
        }
    }
    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion

#region ⬤ Feather ━━━━━
public class HomingFeather : Projectile {
    public Actor? targetFromLock;
    public Point lastMoveAmount;
    const float maxSpeed = 200;
    public HomingFeather(
        Point pos, int xDir, Actor? targetFromLock, Actor owner, Player player, ushort? netId, float? angle = null, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4pck_1atk_feather_proj", netId, player
    ) {
        weapon = BlastHornet.getWeapon();
        damager.damage = 0.1f;
        // damager.flinch = Global.defFlinch;
        this.byteAngle = byteAngle;
        maxTime = 3f;
        projId = (int)ProjIds.BHornetHomingBee;
        destroyOnHit = true;
        shouldShieldBlock = true;
        this.targetFromLock = targetFromLock;
        xScale = xDir == -1 ? -1 : 1;
        this.angle = this.xDir == -1 ? 180 : 0;


        if (targetFromLock == null) {
            targetFromLock = Global.level.getClosestTarget(pos, player.alliance, false, 550);
        }
        if (targetFromLock == null) {
            vel = new Point(xDir, 2).normalize().times(150);
        }

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
        canBeLocal = false;
    }
    public static Projectile rpcInvoke(ProjParameters args) {
        return new HomingFeather(
            args.pos, args.xDir, null, args.owner, args.player, args.netId
        );
    }

    public override void preUpdate() {
        base.preUpdate();
        updateProjectileCooldown();
    }

    public override void update() {
        base.update();

        if (!ownedByLocalPlayer) return;

        if (targetFromLock != null && !targetFromLock.destroyed) {
            var dTo = pos.directionTo(targetFromLock.getCenterPos()).normalize();
            var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
            destAngle = Helpers.to360(destAngle);
            angle = Helpers.lerpAngle(angle, destAngle, Global.spf * 5);
        }
        vel.x = Helpers.cosd(angle) * maxSpeed;
        vel.y = Helpers.sind(angle) * maxSpeed;
    }

    public override void onDestroy() {
        base.onDestroy();

        new HomingFeatherExplo(pos, xDir, this, owner, owner.getNextActorNetId(), rpc: true);
    }

}
#endregion
#region ⬤ Explo ━━━━━━
public class HomingFeatherExplo : Projectile {
    private List<Point> partPositions = new List<Point>() {
        new Point(-16, -16),
        new Point(0, -16),
        new Point(16, -16),
        new Point(-16, 0),
        new Point(0, 0),
        new Point(16, 0),
        new Point(-16, 16),
        new Point(0, 16),
        new Point(16, 16)
    };

    private Random random = new Random();

    public HomingFeatherExplo(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4pck_1atk_feather_explo_proj", netId, player
    ) {
        weapon = BlastHornet.getWeapon();
        projId = (int)ProjIds.BHornetHomingBee;
        damager.damage = 4;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 40;
        vel = Point.zero;
        maxTime = 1.2f;
        destroyOnHit = false;
        destroyOnHitWall = false;

        partPositions = new List<Point>(partPositions);

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new HomingFeatherExplo(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }

    float partTime;

    public override void update() {
        base.update();

        partTime += Global.spf;
        if (partTime >= 0.12f && partPositions.Count > 0) {
            partTime = 0;

            int randomIndex = random.Next(partPositions.Count);
            Point selectedPos = partPositions[randomIndex];

            partPositions.RemoveAt(randomIndex);

            new Anim(
                pos.addxy(selectedPos.x * xDir, selectedPos.y),
                "mav_x4pck_1atk_feather_explo_part", 1, null, true
            ) {
                ttl = 0.3f
            };
        }

    }

    public override void onDestroy() {
        base.onDestroy();
    }
}
#region ⬤ Tail Blade ━━━━
public class TailBladeProj : Projectile {
    public X4Peacock picapau = null!;
    public TailBladeProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4pck_2spc_tail_proj", netId, player
    ) {
        picapau = owner as X4Peacock ?? throw new NullReferenceException();
        weapon = BlastHornet.getWeapon();
        projId = (int)ProjIds.BubbleSplash;
        damager.damage = 5;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        vel = new Point(0 * xDir, 0);
        maxTime = 1.2f;
        destroyOnHit = false;
        destroyOnHitWall = false;
        fadeSprite = "mmx_x4btr_lv0_fade";

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new TailBladeProj(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
        visible = Global.isOnFrameCycle(3);
    }
    public override void postUpdate() {
        base.postUpdate();
        if (owner?.character != null) {
            changePos(owner.character.pos);
        }
    }
    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion
#endregion

#endregion
