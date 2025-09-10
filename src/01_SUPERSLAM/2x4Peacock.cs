using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SFML.Graphics;

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
        if (state is MIdle or MRun or MLand or MJump or MFall) {
            if (input.isPressed(Control.Jump, player)) {
                changeState(new PeacockHover());
            }
        }
        if (input.isPressed(Control.Shoot, player)) {
            if (cursor != null) {
                if (cursor.targetLocked != null) {
                    changeState(new PeacockFireFeather(cursor.targetLocked, fromLoop: false));
                }
            } else {
                changeState(new PeacockFireCursor(startedGrounded: this.grounded));
            }
        }
        if (input.isPressed(Control.Special1, player)) {
            if (cursor != null) {
                if (cursor.targetLocked != null) {
                    changeState(new PeacockRaku(cursor.targetLocked));
                }
            } else {
                changeState(new PeacockFireCursor(startedGrounded: this.grounded));
            }
        }
        if (input.isPressed(Control.Dash, player)) {
            if (cursor != null) {
                if (cursor.targetLocked != null) {
                    changeState(new PeacockTailJump(cursor.targetLocked));
                }
            } else {
                changeState(new PeacockFireCursor(startedGrounded: this.grounded));
            }
        }
        if (input.isPressed(Control.Special2, player)) {
            if (cursor != null) {
                if (cursor.targetLocked != null) {
                    changeState(new PeacockFireGiga(cursor.targetLocked));
                }
            } else {
                changeState(new PeacockFireCursor(startedGrounded: this.grounded));
            }
        }

    }
    #endregion





    public override float getRunSpeed() {

        return 125;
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
    float horizontalInputMove = 125;

    public PeacockHover() : base("fly") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        picapau = maverick as X4Peacock ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.useGravity = false;
    }

    public override void update() {
        base.update();
        controller();

    }
    private void controller() {
        // ----------------- Movement -----------------------
        maverick.move(new Point((player.input.getInputDir(this.player).x * horizontalInputMove), 0));

        if (player.input.isPressed(Control.Jump, player) && stateTime > 0.2f) {
            maverick.changeState(new MFall());
        }
        //  ------------------- Attack -----------------------
        if (player.input.isPressed(Control.Shoot, player)) {
            if (picapau.cursor != null) {
                if (picapau.cursor.targetLocked != null) {
                    maverick.changeState(new PeacockFireFeather(picapau.cursor.targetLocked, fromLoop: true));
                }
            } else {
                maverick.changeState(new PeacockFireCursor(startedGrounded: maverick.grounded));
            }
        }
        if (player.input.isPressed(Control.Special1, player)) {
            if (picapau.cursor != null) {
                if (picapau.cursor.targetLocked != null) {
                    maverick.changeState(new PeacockRaku(picapau.cursor.targetLocked));
                }
            } else {
                maverick.changeState(new PeacockFireCursor(startedGrounded: maverick.grounded));
            }
        }
        if (player.input.isPressed(Control.Dash, player)) {
            if (picapau.cursor != null) {
                if (picapau.cursor.targetLocked != null) {
                    maverick.changeState(new PeacockTailJump(picapau.cursor.targetLocked));
                }
            } else {
                maverick.changeState(new PeacockFireCursor(startedGrounded: maverick.grounded));
            }
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
    bool wasHovering;
    bool hasShot;
    float horizontalInputMove = 80;

    public PeacockFireCursor(bool startedGrounded) : base(startedGrounded ? "1atk_cursor" : "1atk_cursor_air") {
        landSprite = "1atk_feather";
        airSprite = "1atk_feather_air";
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        picapau = maverick as X4Peacock ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.useGravity = false;
        if (oldState is PeacockHover) wasHovering = true;
    }
    public override void update() {
        base.update();
        if (!maverick.grounded) {
            maverick.move(new Point((player.input.getInputDir(this.player).x * horizontalInputMove), 0));
        }
        if (maverick.frameIndex >= 0 && !hasShot) {
            hasShot = true;
            fireCursor();
        }
        if (maverick.isAnimOver()) {
            if (wasHovering) {
                maverick.changeState(new PeacockHover());
            } else {
                maverick.changeToIdleOrFall();
            }
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

#region ■ Fire Feather ━━━
public class PeacockFireFeather : MaverickState {
    public X4Peacock picapau = null!;
    Actor? targetFromLock;
    bool wasHovering;
    bool hasShot;
    bool fromLoop;
    float horizontalInputMove = 80;

    public PeacockFireFeather(Actor? targetFromLock, bool fromLoop) : base("1atk_feather") {
        this.targetFromLock = targetFromLock;
        this.fromLoop = fromLoop;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        picapau = maverick as X4Peacock ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.useGravity = false;
        if (oldState is PeacockHover) wasHovering = true;
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
                maverick.changeState(new PeacockFireFeather(picapau.cursor.targetLocked, fromLoop: true));
            }
        }
        if (maverick.isAnimOver()) {
            if (wasHovering) {
                maverick.changeState(new PeacockHover());
            } else {
                maverick.changeToIdleOrFall();
            }
        }
        if (maverick.frameIndex >= 10) {
            attackCtrl = true;
        }
    }
    private void movementControl() {

        if (!maverick.grounded) {
            maverick.move(new Point((player.input.getInputDir(this.player).x * horizontalInputMove), 0));
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

#region ■ State Raku ━━━━
public class PeacockRaku : MaverickState {
    public X4Peacock picapau = null!;
    Actor? targetFromLock;

    bool hasTeleported;
    public PeacockRaku(Actor? targetFromLock) : base("2spc_raku") {
        this.targetFromLock = targetFromLock;
        superArmor = true;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        picapau = maverick as X4Peacock ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.useGravity = false;

    }
    bool hasFired;
    public override void update() {
        base.update();

        if (maverick.frameIndex >= 7 && !hasTeleported) {
            maverick.useGravity = true;

            Point teleportPos = targetFromLock.pos;

            var groundHit = Global.level.raycast(targetFromLock.pos, targetFromLock.pos.addxy(0, 1000), new List<Type>() { typeof(Wall) });

            if (groundHit?.hitData?.hitPoint != null) {
                teleportPos = groundHit.hitData.hitPoint.Value;
            }

            maverick.changePos(teleportPos);
            hasTeleported = true;
        }
        if (maverick.frameIndex >= 17 && !hasFired) {
            hasFired = true;
            new PeacockRakuProj(maverick.pos, maverick.xDir, maverick, player, player.getNextActorNetId(), true);
        }
        if (maverick.isAnimOver()) {
            maverick.changeState(new MIdle());
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        maverick.useGravity = true;
    }
}
#endregion

#region ■ Tail Jump ━━━━
public class PeacockTailJump : MaverickState {
    public X4Peacock picapau = null!;
    Actor? targetFromLock;
    TailBladeProj? proj;
    bool hasTeleported;
    bool hasCreatedProj;
    float verticalMovement = -500;
    float horizontalInputMove = 50;

    public PeacockTailJump(Actor? targetFromLock) : base("3dash_jump") {
        this.targetFromLock = targetFromLock;
        superArmor = true;
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
        if (maverick.frameIndex >= 15) {
            maverick.move(new Point((player.input.getInputDir(this.player).x * horizontalInputMove), 0));
            if (!hasCreatedProj) {
                hasCreatedProj = true;
                proj = new TailBladeProj(maverick.pos, maverick.xDir, maverick, player, player.getNextActorNetId(), true);
            }
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


#region ■ State Giga ━━━
public class PeacockFireGiga : MaverickState {
    public X4Peacock picapau = null!;
    Actor? targetFromLock;
    bool wasHovering;
    bool hasShot;
    float horizontalInputMove = 80;

    public PeacockFireGiga(Actor? targetFromLock) : base("1atk_feather") {
        this.targetFromLock = targetFromLock;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        picapau = maverick as X4Peacock ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.useGravity = false;
        if (oldState is PeacockHover) wasHovering = true;
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
                maverick.changeState(new PeacockFireFeather(picapau.cursor.targetLocked, fromLoop: true));
            }
        }
        if (maverick.isAnimOver()) {
            if (wasHovering) {
                maverick.changeState(new PeacockHover());
            } else {
                maverick.changeToIdleOrFall();
            }
        }
        if (maverick.frameIndex >= 10) {
            attackCtrl = true;
        }
    }
    private void movementControl() {

        if (!maverick.grounded) {
            maverick.move(new Point((player.input.getInputDir(this.player).x * horizontalInputMove), 0));
        }
    }
    private void fireFeather() {
        Point? shootPos = maverick.getFirstPOIOrDefault();
        if (shootPos != null) {
            new FinalWeaponLockProj(
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
                targetHoming = Global.level.getClosestTarget(pos, damager.owner.alliance, true, aMaxDist: 600);
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
        this.targetFromLock = targetFromLock;
        weapon = BlastHornet.getWeapon();
        damager.damage = 0.1f;
        // damager.flinch = Global.defFlinch;
        this.byteAngle = byteAngle;
        maxTime = 3f;
        projId = (int)ProjIds.BHornetHomingBee;
        destroyOnHit = true;
        shouldShieldBlock = true;
        xScale = xDir == -1 ? -1 : 1;
        this.angle = this.xDir == -1 ? 180 : 0;
        new Anim(pos, "mav_x4pck_1atk_feather_muzzle", xDir, null, true);

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
    public override void onStart() {
        base.onStart();

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
        new Point(-16, -16), new Point(0, -16),new Point(16, -16),
        new Point(-16, 0), new Point(0, 0), new Point(16, 0),
        new Point(-16, 16), new Point(0, 16), new Point(16, 16)
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
#endregion
#region ⬤ RakuProj ━━━━━
public class PeacockRakuProj : Projectile {
    public PeacockRakuProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4pck_2spc_raku_proj", netId, player
    ) {
        weapon = FakeZero.getWeapon();
        projId = (int)ProjIds.FakeZeroBuster;
        damager.damage = 2;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 15;
        vel = Point.zero;
        maxTime = 0.7f;
        destroyOnHit = false;
        destroyOnHitWall = false;

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new PeacockRakuProj(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }



    public override void update() {
        base.update();
        if (isAnimOver()) destroySelf();
    }

    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion



#region ⬤ Teleport Proj ━━━
public class PeacockTeleportProj : Projectile {
    //public int type;
    bool hasHitGround;
    public PeacockTeleportProj(
        Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "empty", netId, player
    ) {
        //this.type = type;
        weapon = FakeZero.getWeapon();
        projId = (int)ProjIds.GaeaShield;
        damager.damage = 0;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        vel = Point.zero;
        maxTime = 0.4f;
        destroyOnHit = false;
        destroyOnHitWall = false;
        globalCollider = new Collider(
            new Rect(0, 0, 15, 15).getPoints(),
            true, this, false, false, HitboxFlag.Hitbox, Point.zero
        );
        switch (type) {
            case 0:
                vel = new Point(-900, 700);
                break;
            case 1:
                vel = new Point(0, 700);
                break;
            case 2:
                vel = new Point(900, 700);
                break;
        }

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new PeacockTeleportProj(
            args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
        );
    }

    float partTime;
    public override void update() {
        base.update();
        partTime += Global.spf;
        if (partTime >= 0.02f && !hasHitGround) {
            partTime = 0;
            new Anim(pos.addxy(0, -25), "mav_x4pck_2spc_teleport_fx", 1, null, true) {
                //kill sprite before it loops, the loop is used on the proj sprite
                ttl = 0.23f
            };
        }
    }
    public override void onHitWall(CollideData other) {
        base.onHitWall(other);
        if (other.isSideWallHit()) {
            vel = new Point(0, 700);
        }
        if (other.isGroundHit()) {
            vel = Point.zero;
            hasHitGround = true;
        }
    }
    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion

#region ⬤ Tail Blade ━━━━
public class TailBladeProj : Projectile {
    public X4Peacock picapau = null!;
    public TailBladeProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4pck_3dash_tail_proj", netId, player
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
#region ⬤ Giga Lock ━━━━
public class FinalWeaponLockProj : Projectile {
    public Actor? targetFromLock;

    private int frameCount;
    private float angle = 0;
    float lineHeight = 360f;
    private float initialRadius = 60f;

    private const float BOTMID_Y_FIX = 180f;

    public FinalWeaponLockProj(
        Point pos, int xDir, Actor? targetFromLock, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "empty", netId, player
    ) {
        this.targetFromLock = targetFromLock;
        weapon = BlastHornet.getWeapon();
        projId = (int)ProjIds.BubbleSplash;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        vel = Point.zero;
        maxTime = 2f;
        destroyOnHit = true;
        destroyOnHitWall = false;

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new FinalWeaponLockProj(
            args.pos, args.xDir, null, args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
        frameCount++;
        Point teleportPos = targetFromLock.pos.addxy(0, 239);

        var groundHit = Global.level.raycast(targetFromLock.pos, targetFromLock.pos.addxy(0, 240), new List<Type>() { typeof(Wall) });

        if (groundHit?.hitData?.hitPoint != null) {
            teleportPos = groundHit.hitData.hitPoint.Value;
        }
        if (time <= 1.5f) {
            this.changePos(teleportPos);
        }
    }
    public override void postUpdate() {
        base.postUpdate();
        if (!ownedByLocalPlayer) return;

        if (targetFromLock != null) {
            if (time <= 1.5f) {
                drawConverginLines();
            } else {
                drawFlashingLine();
            }
        }
    }
    private void drawConverginLines() {
        angle += Global.spf * 200f;

        if (angle >= 360) {
            angle = 0;
        }
        // Calculate the current radius. It will decrease as the projectile's time approaches its maxTime.
        float currentRadius = initialRadius * (1.5f - time) / 1.5f;

        // The sign of verticalRadius controls the perspective. Positive makes the front lines curve outwards.
        float verticalRadius = 12f;

        for (int i = 0; i < 4; i++) {
            // Calculate the angle for each of the four lines, spaced 90 degrees apart.
            float lineAngle = angle + (i * 90);
            // Convert the angle to radians for trigonometric functions.
            float angleInRadians = lineAngle * (float)Math.PI / 180f;

            // Calculate the X position of the line on the circle.
            float currentX = pos.x + currentRadius * (float)Math.Cos(angleInRadians);

            // Adjust the Y values to create a cylinder shape
            float y1 = pos.y - (lineHeight / 2) + verticalRadius * (float)Math.Sin(angleInRadians);
            float y2 = pos.y + (lineHeight / 2) - verticalRadius * (float)Math.Sin(angleInRadians);

            DrawWrappers.DrawLine(currentX, y1 - BOTMID_Y_FIX, currentX, y2 - BOTMID_Y_FIX, Color.Red, 1f, ZIndex.HUD, true);
        }
    }

    private void drawFlashingLine() {
        if (frameCount % 3 == 0) {
            float convergedX = pos.x;
            float y1 = pos.y - (lineHeight / 2);
            float y2 = pos.y + (lineHeight / 2);
            DrawWrappers.DrawLine(convergedX, y1 - BOTMID_Y_FIX, convergedX, y2 - BOTMID_Y_FIX, Color.Red, 4f, ZIndex.HUD, true);
        }
    }

    public override void onDestroy() {
        base.onDestroy();
        for (int i = 0; i < 5; i++) {
            Point piecePos = new Point(pos.x, pos.y - (30 + (i * 64)));
            int type = 1; // Default to the center type
            if (i == 0) {
                type = 2; // The first piece (start)
            } else if (i == 4) {
                type = 0; // The last piece (end)
            }
            new FinalWeaponPieceProj(piecePos, xDir, type, this, damager.owner, Global.level.mainPlayer.getNextActorNetId(), rpc: true);
        }
    }
}
#endregion
#region ⬤ Giga Piece ━━━━
public class FinalWeaponPieceProj : Projectile {
    public int type;

    int customLoopCount;
    private const int MAX_ANIM_LOOPS = 6;
    public FinalWeaponPieceProj(
        Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "empty", netId, player
    ) {
        this.type = type;
        // weapon = NewBuster.netWeapon;
        // projId = (int)ProjIds.BusterLv0Proj;
        damager.damage = 3;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 12;
        vel = Point.zero;
        maxTime = 3f;
        destroyOnHit = false;
        destroyOnHitWall = false;
        fadeOnAutoDestroy = true;

        switch (type) {
            case 0: //start
                changeSprite("mav_x4pck_4giga_head", false);
                break;
            case 1: //piece
                changeSprite("mav_x4pck_4giga_piece", false);
                break;
            case 2: //end (land)
                yScale = -1;
                changeSprite("mav_x4pck_4giga_head", false);
                break;
        }

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new FinalWeaponPieceProj(
            args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
        );
    }
    public override void onStart() {
        base.onStart();
        shakeCamera();
    }
    float partTime = 1;
    public override void update() {
        base.update();

        visible = Global.isOnFrameCycle(3);
        if (isAnimOver()) destroySelf();
        if (type == 2) { //part
            partTime += Global.spf;
            if (partTime >= 0.05f) {
                partTime = 0;
                new Anim(pos.addxy(Helpers.randomRange(-10, 30) * xDir, Helpers.randomRange(10, 30)), "mav_x4pck_1atk_feather_explo_part", 1, null, true) {
            ttl = 0.3f,
                    zIndex = this.zIndex - 30,
                    vel = new Point(0, -300), //reverse soeed
                };
            }
        }
        //just a custom loop
        if (frameIndex == 8 && customLoopCount < MAX_ANIM_LOOPS) {
            customLoopCount++;
            frameIndex = 4;

        }
    }

    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion
#endregion

/*
tomorrow
- raycast on point, start creating all GigaLock from ground 
- handle edgecase when there is no ground
- sprite head shape
 
-
-

-piece type0
-piece creates pillar type1

- type0 parts "mav_x4pck_1atk_feather_explo_proj"







*/