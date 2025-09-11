using System;
using System.Collections.Generic;
namespace MMXOnline;

#region ▄▄★ TEMPLATE ★▄▄ 

public class X4Stingray : Maverick {
    public static Weapon getWeapon() { return new Weapon(WeaponIds.FakeZeroGeneric, 150); }

    // Main creation function.
    public X4Stingray(
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
    }


    public override void update() {
        base.update();

        if (!ownedByLocalPlayer) return;

    }

    public override bool attackCtrl() {
        if (input.isHeld(Control.Shoot, player) && state is MRun) {
            changeState(new FakeZeroMeleeState());
            return true;
        }
        if (input.isHeld(Control.Shoot, player)) {
            changeState(new FakeZeroShootState(), false);
            return true;
        }
        if (input.isPressed(Control.Special1, player)) {
            changeState(new FakeZeroMeleeState());
            return true;
        }
        if (input.isPressed(Control.Dash, player)) {
            changeState(new FakeZeroGroundPunchState());
            return true;
        }
        if (grounded) {
            if (input.isHeld(Control.Down, player) && state is not FakeZeroGuardState) {
                changeState(new FakeZeroGuardState());
                return true;
            }
        }
        return false;
    }


    public override float getRunSpeed() {

        return 111;
    }

    public override string getMaverickPrefix() {
        return "mav_x4";
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







#region ■ Knee Shoot ━━━
public class StingrayKneeShoot : MaverickState {
    public X4Stingray hipo = null!;
    bool hasFired;



    public StingrayKneeShoot() : base("1atk") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        hipo = maverick as X4Stingray ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.useGravity = false;
    }

    public override void update() {
        base.update();
        if (maverick.frameIndex == 4 && !hasFired) {
            hasFired = true;
            //new stingray shot (constructor stingray hp)
        }
        if (maverick.frameIndex == 7 && player.input.isHeld(Control.Shoot, player)) {
            hasFired = false;
            maverick.frameIndex = 4;
        }
        if (maverick.isAnimOver()) {
            maverick.changeState(new MIdle());
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        maverick.useGravity = false;
    }
}
#endregion
#region ■ Whirpool Shoot ━
public class StingrayWhirpool : MaverickState {
    public X4Stingray hipo = null!;
    bool startedGrounded;

    Projectile? backwardsProj;
    Projectile? fowardsProj;

    private int whirpoolType = 0;

    private bool isMovingLeft;
    private bool isMovingRight;
    private bool hasCreatedBackwards;
    private bool hasCreatedFowards;

    private const float MOVE_SPEED = 80;

    public StingrayWhirpool(bool startedGrounded) : base(startedGrounded ? "1atk" : "1atk_air") {
        this.startedGrounded = startedGrounded;
        landSprite = "2spcl"; //start jsons
        airSprite = "2spcl_air";

    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        hipo = maverick as X4Stingray ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.useGravity = false;
    }

    public override void update() {
        base.update();
        if (maverick.frameIndex < 5) return;
        isMovingLeft = false;
        isMovingRight = false;

        if (player.input.isHeld(Control.Left, player)) {
            maverick.changeSpriteFromName("2spcl_backwards", true);
            isMovingLeft = true;
            whirpoolType = 0;
            maverick.move(new Point(maverick.grounded ? 0 : -MOVE_SPEED, 0));

        } else if (player.input.isHeld(Control.Right, player)) {
            maverick.changeSpriteFromName("2spcl_fowards", true);
            isMovingRight = true;
            whirpoolType = 1;
            maverick.move(new Point(maverick.grounded ? 0 : MOVE_SPEED, 0));
        }
        if (isMovingLeft && !hasCreatedBackwards) {
            hasCreatedBackwards = true;
            hasCreatedFowards = false;
            // backwardsProj = New
            fowardsProj?.destroySelf();

        } else if (isMovingRight && !hasCreatedFowards) {
            hasCreatedFowards = true;
            hasCreatedBackwards = false;
            // fowardsProj = New
            backwardsProj?.destroySelf();
        }
        if (!player.input.isHeld(Control.Left, player) || !player.input.isHeld(Control.Right, player)) {
            maverick.changeState(new MIdle());
        }
    }


    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        maverick.useGravity = true;
    }
}
#endregion

#region ■ Flying Dash ━━━
public class StingrayFlyingDash : MaverickState {   //check old
    public X4Stingray hipo = null!;
    //bool startedGrounded;

    public StingrayFlyingDash() : base("1atk") {
        //public MavState(bool startedGrounded) : base(startedGrounded ? "1atk" : "1atk_air") {
        //this.startedGrounded = startedGrounded;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        hipo = maverick as X4Stingray ?? throw new NullReferenceException();
        maverick.stopMoving();
    }

    public override void update() {
        base.update();

        if (maverick.isAnimOver()) {
            maverick.changeState(new MIdle());
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
    }
}
#endregion

#region ■ Giga Shoot ━━━
public class StingrayGiga : MaverickState {
    public X4Stingray hipo = null!;
    //bool startedGrounded;

    public StingrayGiga() : base("1atk") {
        //public MavState(bool startedGrounded) : base(startedGrounded ? "1atk" : "1atk_air") {
        //this.startedGrounded = startedGrounded;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        hipo = maverick as X4Stingray ?? throw new NullReferenceException();
        maverick.stopMoving();
    }

    public override void update() {
        base.update();

        if (maverick.isAnimOver()) {
            maverick.changeState(new MIdle());
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
    }
}
#endregion



#endregion

#region ▄▄▄▄⬤ PROJ ⬤▄▄▄▄








#region ⬤ Knee Proj ━━━━
public class StingrayKneeProj : Projectile {
    public StingrayKneeProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "empty", netId, player
    ) {
        // weapon = NewBuster.netWeapon;
        // projId = (int)ProjIds.BusterLv0Proj;
        vel = new Point(300 * xDir, 0);
        maxTime = 1.2f;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        //----------------------------//    
        destroyOnHit = true;
        destroyOnHitWall = false;
        fadeSprite = "mmx_x4btr_lv0_fade";
        fadeOnAutoDestroy = true;

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new StingrayKneeProj(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
    }

    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion

#region ⬤ Whirpool Proj ━━
public class StingrayWhirpoolProj : Projectile {
    //public int type;
    public StingrayWhirpoolProj(
        Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "empty", netId, player
    ) {
        //weapon = NewBuster.netWeapon;
        //projId = (int)ProjIds.BusterLv0Proj;
        vel = new Point(300 * xDir, 0);
        maxTime = 1.2f;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        //----------------------------//    
        //this.type = type;
        destroyOnHit = true;
        destroyOnHitWall = false;
        fadeSprite = "mmx_x4btr_lv0_fade";
        fadeOnAutoDestroy = true;

        switch (type) {
            case 0:
                break;
            case 1:
                break;
        }

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new TypedProj(
            args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
    }

    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion

#region ⬤ Dash Proj ━━━━
public class StingrayDashProj : Projectile {
    public StingrayDashProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "empty", netId, player
    ) {
        // weapon = NewBuster.netWeapon;
        // projId = (int)ProjIds.BusterLv0Proj;
        vel = new Point(300 * xDir, 0);
        maxTime = 1.2f;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        //----------------------------//    
        destroyOnHit = true;
        destroyOnHitWall = false;
        fadeSprite = "mmx_x4btr_lv0_fade";
        fadeOnAutoDestroy = true;

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new StingrayDashProj(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
    }

    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion

#region ⬤ Giga Proj ━━━━
public class StingrayGigaProj : Projectile {
    //public int type;
    public StingrayGigaProj(
        Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "empty", netId, player
    ) {
        //weapon = NewBuster.netWeapon;
        //projId = (int)ProjIds.BusterLv0Proj;
        vel = new Point(300 * xDir, 0);
        maxTime = 1.2f;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        //----------------------------//    
        //this.type = type;
        destroyOnHit = true;
        destroyOnHitWall = false;
        fadeSprite = "mmx_x4btr_lv0_fade";
        fadeOnAutoDestroy = true;

        switch (type) {
            case 0:
                break;
            case 1:
                break;
        }

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new StingrayGigaProj(
            args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
    }

    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion




#endregion
