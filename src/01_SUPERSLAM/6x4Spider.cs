using System;
using System.Collections.Generic;
namespace MMXOnline;

#region ▄▄★ TEMPLATE ★▄▄ 

public class X4Spider : Maverick {
    public static Weapon getWeapon() { return new Weapon(WeaponIds.FakeZeroGeneric, 150); }

    // Main creation function.
    public X4Spider(
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


    #region ★ Update ━━━━━
    public override void update() {
        base.update();
        if (!ownedByLocalPlayer) return;

        //check terrain up
        //              down
        //              left
        //              right

        // if (input.isPressed(Control.Up, player) && terrainUp) {
        //     changeState(new SpiderNormalWallCrawl(up));

        // } else if (input.isPressed(Control.Down, player) && terrainDown) {
        //     changeState(new SpiderNormalWallCrawl(down));

        // } else if (input.isPressed(Control.Left, player) && terrainLeft) {
        //     changeState(new SpiderNormalWallCrawl(left));

        // } else if (input.isPressed(Control.Right, player) && terrainRight) {
        //     changeState(new SpiderNormalWallCrawl(right));
        // }

        if (state is MJump && input.isPressed(Control.Jump, player) /* && check backwallcenterpos true*/) {
            changeState(new SpiderBackWallCrawl());
        }

        if (state is MIdle or MRun or MLand or MJump or MFall) {

            if (input.isPressed(Control.Shoot, player)) {

            }
            if (input.isPressed(Control.Special1, player)) {

            }
            if (input.isPressed(Control.Dash, player)) {

            }
            if (input.isPressed(Control.Special2, player)) {

            }
        }
    }
    #endregion

    public override float getRunSpeed() {

        return 65;
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







#region ■ Back Wall ━━━
public class SpiderBackWallCrawl : MaverickState {
    public X4Spider culona = null!;

    public SpiderBackWallCrawl() : base("1atk") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        culona = maverick as X4Spider ?? throw new NullReferenceException();
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

#region ■ Normal Wall ━━━
public class SpiderNormalWallCrawl : MaverickState {
    public X4Spider culona = null!;

    public SpiderNormalWallCrawl() : base("1atk") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        culona = maverick as X4Spider ?? throw new NullReferenceException();
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

#region ■ Attack Web ━━━
public class SpiderAttack : MaverickState {
    public X4Spider culona = null!;
    Anim? chooseWheel;
    Anim? chooseArrow;

    private bool hasStoppedHolding;
    private bool hasFired;
    private int webType;


    public SpiderAttack() : base("1atk") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        culona = maverick as X4Spider ?? throw new NullReferenceException();
        maverick.stopMoving();
        // chooseWheel = new anim mav pos ➩
    }

    public override void update() {
        base.update();
        if (input.isHeld(Control.Shoot, player)) {
            webDirectionByInput();
        } else if (!input.isHeld(Control.Shoot, player)) {
            maverick.frameSpeed = 1;
        }

        if (maverick.frameIndex == 69 && !hasFired) {
            hasFired = true;
            // new homing web web type
        }

        if (maverick.isAnimOver()) {
            maverick.changeState(new MIdle());
        }
    }
    private void webDirectionByInput() {

        Point inputDir = player.input.getInputDir(player);

        if (inputDir.y == 1 && inputDir.x == 0) {           // ↑ 
            webType = 0;
        } else if (inputDir.y > 0 && inputDir.x > 0) {      // ↗ 
            webType = 1;
        } else if (inputDir.y == 0 && inputDir.x == 1) {    // →
            webType = 2;
        } else if (inputDir.y < 0 && inputDir.x > 0) {      // ↘ 
            webType = 3;
        } else if (inputDir.y == -1 && inputDir.x == 0) {   // ↓
            webType = 4;
        } else if (inputDir.y < 0 && inputDir.x < 0) {      // ↙ 
            webType = 5;
        } else if (inputDir.y == 0 && inputDir.x == -1) {   // ← 
            webType = 6;
        } else if (inputDir.y > 0 && inputDir.x < 0) {      // ↖ 

        }
    }


    public override void onExit(MaverickState newState) {
        base.onExit(newState);
    }
}
#endregion

#region ■ Shoot Spiders ━━
public class SpiderShootSpiders : MaverickState {
    public X4Spider culona = null!;

    private float launchStrenght;
    public SpiderShootSpiders() : base("1atk") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        culona = maverick as X4Spider ?? throw new NullReferenceException();
        maverick.stopMoving();
    }

    public override void update() {
        base.update();

        launchStrenght += Global.spf * 20f;

        if (!player.input.isHeld(Control.Special1, player)) {
            for (int i = 0; i < 4; i++) {
                // new spider ( laucnchspeed)
            }
        }
        if (maverick.isAnimOver()) {
            maverick.changeState(new MIdle());
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
    }
}
#endregion

#region ■ Shoot Sling  ━━━
public class SpiderShootSling : MaverickState {
    public X4Spider culona = null!;

    public SpiderShootSling() : base("1atk") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        culona = maverick as X4Spider ?? throw new NullReferenceException();
        maverick.stopMoving();

    }

    public override void update() {
        base.update();
        if (maverick.frameIndex == 69) {
            //new sling
        }

        if (maverick.isAnimOver()) {
            maverick.changeState(new MIdle());
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
    }
}
#endregion

#region ■ Swinging  ━━━━
public class SpiderSwinging : MaverickState {
    public X4Spider culona = null!;

    public SpiderSwinging() : base("1atk") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        culona = maverick as X4Spider ?? throw new NullReferenceException();
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

#region ■ Giga  ━━━━━━━
public class SpiderGiga : MaverickState {
    public X4Spider culona = null!;

    public SpiderGiga() : base("1atk") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        culona = maverick as X4Spider ?? throw new NullReferenceException();
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






#region ⬤ Homing Web ━━━
public class SpiderHomingWebProj : Projectile {
    //public int type;
    public SpiderHomingWebProj(
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
            case 2:
                break;
            case 3:
                break;
            case 4:
                break;
            case 5:
                break;
            case 6:
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

#region ⬤ Mini Spider ━━━
public class SpiderMiniSpiderProj : Projectile {
    //public int type;
    public SpiderMiniSpiderProj(
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

#region ⬤ Sling Proj ━━━
public class SpiderSlingProj : Projectile {
    //public int type;
    public SpiderSlingProj(
        Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "buster1", netId, player
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
            case 2:
                break;
            case 3:
                break;
            case 4:
                break;
            case 5:
                break;
            case 6:
                break;
        }

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new SpiderSlingProj(
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

#region ⬤ BackWall Web ━━
public class AngledProj : Projectile {
    public AngledProj(
        Point pos, int xDir, float byteAngle, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "empty", netId, player
    ) {
        //  weapon = NewBuster.netWeapon;
        // projId = (int)ProjIds.BusterLv0Proj;
        vel.x = 400 * Helpers.cosb(byteAngle);
        vel.y = 400 * Helpers.sinb(byteAngle);
        maxTime = 1.2f;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        //----------------------------//    
        byteAngle = byteAngle % 256;
        this.byteAngle = byteAngle;
        destroyOnHit = true;
        destroyOnHitWall = false;
        fadeSprite = "mmx_x4btr_lv0_fade";
        fadeOnAutoDestroy = true;

        if (rpc) {
            rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new AngledProj(
            args.pos, args.xDir, args.byteAngle, args.owner, args.player, args.netId
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
