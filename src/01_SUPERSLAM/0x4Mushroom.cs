using System;
using System.Collections.Generic;
namespace MMXOnline;

#region ▄★ MUSHROOM ★▄

public class X4Mushroom : Maverick {
    public static Weapon getWeapon() { return new Weapon(WeaponIds.FakeZeroGeneric, 150); }


    // Main creation function.
    public X4Mushroom(
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
        gravityModifier = 0.7f;
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
        if (state is MIdle or MRun or MLand) {
            if (input.isPressed(Control.Jump, player)) {
                changeState(new MushroomJump(0, isDoubleJump: false));
            }
            if (input.isHeld(Control.Down, player)) {
                changeState(new MushroomCroutch());
            }
            if (input.isHeld(Control.Left, player)) {
                xDir = -1;
                changeState(new MushroomRun(0));

            } else if (input.isHeld(Control.Right, player)) {
                xDir = 1;
                changeState(new MushroomRun(0));
            }

        }
    }
    #endregion
    #region ★ Atk Ctrl ━━━━━
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




        return false;
    }
    #endregion


    public override float getRunSpeed() {

        return 69;
    }

    public override string getMaverickPrefix() {
        return "mav_x4mrm";
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






#region ■ Run ━━━━━━━
public class MushroomRun : MaverickState {
    public X4Mushroom minepe = null!;
    private float storedXSpeed;

    private bool isHoldingDirection;
    private int lastXDir;

    private const float SPEED_ACC = 165f;   //holding fowards
    private const float SPEED_DEACC = 230f; //not holding anything
    private const float SPEED_MAX = 200f;


    public MushroomRun(float storedXSpeed) : base("run") {
        this.storedXSpeed = storedXSpeed;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
    }

    public override void preUpdate() {
        base.preUpdate();
        lastXDir = maverick.xDir;
    }

    public override void update() {
        base.update();

        isHoldingDirection = false;

        //input move
        if (maverick.input.isHeld(Control.Left, maverick.player)) {
            maverick.xDir = -1;
            isHoldingDirection = true;
        } else if (maverick.input.isHeld(Control.Right, maverick.player)) {
            maverick.xDir = 1;
            isHoldingDirection = true;
        }

        maverick.move(new Point((int)(storedXSpeed * maverick.xDir), 0));

        //----------------------Movement Calculation------------------------------------
        //animation speed * math
        maverick.frameSpeed = Math.Max(1f, 1f * (storedXSpeed * 0.014f));

        if (isHoldingDirection) {
            // Accelerate when holding direction
            storedXSpeed = Math.Min(SPEED_MAX, storedXSpeed + (Global.spf * SPEED_ACC));
        } else {
            // Decelerate when NOT holding direction
            storedXSpeed = Math.Max(0, storedXSpeed - (Global.spf * SPEED_DEACC));
        }
        if (lastXDir != maverick.xDir && isHoldingDirection) {
            storedXSpeed = 0;
        }

        if (!isHoldingDirection && storedXSpeed <= 30f) {
            maverick.changeState(new MIdle());
        }
        //Controller
        if (maverick.input.isPressed(Control.Jump, maverick.player)) {
            maverick.changeState(new MushroomJump(storedXSpeed, isDoubleJump: (maverick.grounded ? false : true)));
        }
    }
}

#endregion

#region ■ Jump ━━━━━━━
public class MushroomJump : MaverickState {
    public X4Mushroom minepe = null!;
    MushrenzanProj? proj;
    bool isDoubleJump;
    float storedXSpeed;

    private bool stoppedHoldingJump;
    private bool isHoldingDirection;
    private int lastXDir;

    private const float SPEED_ACC = 165f;   //holding fowards
    private const float SPEED_DEACC = 230f; //not holding anything
    private const float SPEED_MAX = 200f;

    private const float JUMP_MOD = 0.9f;
    private const float JUMP_MOD2 = 1.1f;


    public MushroomJump(float storedXSpeed, bool isDoubleJump) : base(isDoubleJump ? "jump2" : "jump") {
        this.storedXSpeed = storedXSpeed;
        this.isDoubleJump = isDoubleJump;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
        maverick.vel.y = -maverick.getJumpPower() * (isDoubleJump ? JUMP_MOD2 : JUMP_MOD);
        if (isDoubleJump) {
            proj = new MushrenzanProj(maverick.pos, maverick.xDir, maverick, player, player.getNextActorNetId(), true);
        }
    }
    public override void preUpdate() {
        base.preUpdate();
        lastXDir = maverick.xDir;
    }
    public override void update() {
        base.update();

        isHoldingDirection = false;

        //direction change
        if (maverick.input.isHeld(Control.Left, maverick.player)) {
            maverick.xDir = -1;
            isHoldingDirection = true;
        } else if (maverick.input.isHeld(Control.Right, maverick.player)) {
            maverick.xDir = 1;
            isHoldingDirection = true;
        }

        maverick.move(new Point((int)(storedXSpeed * maverick.xDir), 0));

        //----------------------Movement Calculation----------------------------
        if (isHoldingDirection) {
            // Accelerate when holding direction
            storedXSpeed = Math.Min(SPEED_MAX, storedXSpeed + (Global.spf * SPEED_ACC));
        } else {
            // Decelerate when NOT holding direction
            storedXSpeed = Math.Max(0, storedXSpeed - (Global.spf * SPEED_DEACC));
        }
        //without this, changing direction will do so at full storedXSpeed
        if (lastXDir != maverick.xDir && isHoldingDirection) {
            storedXSpeed = -storedXSpeed;
        }
        //---------------------------Jump Controller---------------------------
        //start falling when not holding Control.Jump
        if (stateTime >= 0.05f) {
            if (!player.input.isHeld(Control.Jump, player) || stateTime >= 0.4f)
                if (!stoppedHoldingJump) {
                    stoppedHoldingJump = true;
                    maverick.vel.y = 0;
                    if (!isDoubleJump) {
                        maverick.changeSpriteFromName("fall", true);
                    }
                }
        }
        //double jump
        if (stateTime >= 0.02f && maverick.input.isPressed(Control.Jump, maverick.player)) {
            if (!isDoubleJump) {
                maverick.changeState(new MushroomJump(storedXSpeed, isDoubleJump: true));
            }
        }
        //end when grounded
        if (maverick.grounded && stateTime >= 0.1f) {
            maverick.changeState(new MushroomRun(storedXSpeed));
        }
    }
}
#endregion


#region ■ Croutch ━━━━━
public class MushroomCroutch : MaverickState { //aka spindash start
    public X4Mushroom minepe = null!;
    private bool startedSpindashing;
    private float dashFactor = 0;
    private float timeSinceLastRev = 1f;

    private const float MIN_FACTOR = 100;
    private const float MAX_FACTOR = 400;
    private const float FACTOR_MULT = 1.5f; //when release


    public MushroomCroutch() : base("croutch") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
        maverick.stopMoving();
    }

    public override void update() {
        base.update();

        //------------------------------Croutch Handling------------------------
        // Initiate SpinDash/Croutch
        if (!input.isHeld(Control.Down, player)) {
            if (!startedSpindashing) {
                maverick.changeState(new MIdle());
            } else {
                maverick.changeState(new MushroomRun(dashFactor * FACTOR_MULT));
            }
        }
        //Initiate Revvin'
        if (input.isPressed(Control.Dash, player) && !startedSpindashing) {
            startedSpindashing = true;
            dashFactor = MIN_FACTOR;
            maverick.changeSpriteFromName("3dash_spin", true);
        }

        //------------------------------Factor Handling------------------------

        //Decrease factor over time 
        if (startedSpindashing) {
            maverick.frameSpeed = dashFactor * 0.03f; //anim speed
            timeSinceLastRev -= Global.spf * 3.5f;   // 3.5 equals decrease time mult
        }
        if (timeSinceLastRev <= 0) {
            timeSinceLastRev = 1f;
            dashFactor = Math.Max(MIN_FACTOR, dashFactor - MIN_FACTOR);
        }
        //Increase Factor by Control.Dash
        if (input.isPressed(Control.Dash, player) && startedSpindashing) {
            dashFactor = Math.Min(MAX_FACTOR, dashFactor + MIN_FACTOR);
            timeSinceLastRev = 1f;

            new Anim(maverick.pos.addxy(5 * -maverick.xDir, 0), "dash_sparks",
            maverick.xDir, player.getNextActorNetId(), true, sendRpc: true);
        }
    }
}
#endregion


#region ■ Spin Dash ━━━━
public class MushroomSpinDash : MaverickState {
    public X4Mushroom minepe = null!;


    public MushroomSpinDash() : base("1atk") {
        //constructor comes the spindash speed
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
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

#region ■ Flinch ━━━━━━
public class MushroomFlinch : MaverickState {
    public X4Mushroom minepe = null!;


    public MushroomFlinch() : base("1atk") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
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

#region ■ Headbutt ━━━━
public class MusrhoomHeadbutt : MaverickState {
    public X4Mushroom minepe = null!;
    //bool startedGrounded;

    public MusrhoomHeadbutt() : base("1atk") {
        //public MavState(bool startedGrounded) : base(startedGrounded ? "1atk" : "1atk_air") {
        //this.startedGrounded = startedGrounded;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
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

#region ■ Giga ━━━━━━━
public class MushroomGiga : MaverickState {
    public X4Mushroom minepe = null!;
    //bool startedGrounded;

    public MushroomGiga() : base("1atk") {
        //public MavState(bool startedGrounded) : base(startedGrounded ? "1atk" : "1atk_air") {
        //this.startedGrounded = startedGrounded;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
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







#region ⬤ Mushrenzan ━━━
public class MushrenzanProj : Projectile {
    public X4Mushroom minepe = null!;
    public MushrenzanProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4mrm_jump2_proj", netId, player
    ) {
        minepe = owner as X4Mushroom ?? throw new NullReferenceException();
        weapon = FakeZero.getWeapon();
        projId = (int)ProjIds.GBeetleGravityWell;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        vel = Point.zero;
        maxTime = 0.3f;
        destroyOnHit = false;
        destroyOnHitWall = false;

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new MushroomBodyProj(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
        if (isAnimOver()) destroySelf();
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

#region ⬤ Mushroom Body ━
public class MushroomBodyProj : Projectile {
    public MushroomBodyProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "empty", netId, player
    ) {
        weapon = FakeZero.getWeapon();
        projId = (int)ProjIds.GBeetleGravityWell;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        vel = new Point(300 * xDir, 0);
        maxTime = 1.2f;
        destroyOnHit = true;
        destroyOnHitWall = false;
        fadeSprite = "mmx_x4btr_lv0_fade";

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new MushroomBodyProj(
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

#region ⬤ Fake Mushroom ━
public class TypedProj : Projectile {
    //public int type;
    public TypedProj(
        Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "empty", netId, player
    ) {
        //this.type = type;
        weapon = FakeZero.getWeapon();
        projId = (int)ProjIds.GBeetleGravityWell;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        vel = new Point(300 * xDir, 0);
        maxTime = 1.2f;
        destroyOnHit = true;
        destroyOnHitWall = false;
        fadeSprite = "mmx_x4btr_lv0_fade";

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
#region ⬤ Poison Cloud ━━━
public class NeutralProj : Projectile {
    public NeutralProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "empty", netId, player
    ) {
        weapon = FakeZero.getWeapon();
        projId = (int)ProjIds.GBeetleGravityWell;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        vel = new Point(300 * xDir, 0);
        maxTime = 1.2f;
        destroyOnHit = true;
        destroyOnHitWall = false;
        fadeSprite = "mmx_x4btr_lv0_fade";

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new NeutralProj(
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
#region ⬤ Bouncy Giga ━━━


#endregion



#endregion
