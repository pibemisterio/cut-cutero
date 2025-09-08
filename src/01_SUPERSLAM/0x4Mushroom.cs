using System;
using System.Collections.Generic;
namespace MMXOnline;

#region ▄★ MUSHROOM ★▄

public class X4Mushroom : Maverick {
    public static Weapon getWeapon() { return new Weapon(WeaponIds.FakeZeroGeneric, 150); }
    private float bodyCreation = 0;
    private const float BODY_CD = 0.6f;


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

        gravityModifier = 0.8f;
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
        bodyCreation += Global.spf;
        if (input.isHeld(Control.Shoot, player) && bodyCreation >= BODY_CD && state is not MushroomWall) {
            new MushroomBodyProj(pos, xDir, isFromWall: false, this, player, player.getNextActorNetId(), true);
            bodyCreation = 0;
        }
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
        if (state is MushroomJump or MushroomSpinDash) {
            if (input.isHeld(Control.Down, player) && input.isPressed(Control.Dash, player)) {
                changeState(new MushroomHeadbutt());
            }
        }
    }

    #endregion
    #region ★ Atk Ctrl ━━━━━
    public override bool attackCtrl() {

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

    private const float SPEED_ACC = 180f;   //holding fowards
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

        //---------------------- Movement Calculation ------------------------------

        maverick.frameSpeed = Math.Max(1f, 1f * (storedXSpeed * 0.014f));  //animation speed
        //acc and deacc
        if (isHoldingDirection) {
            storedXSpeed = Math.Min(SPEED_MAX, storedXSpeed + (Global.spf * SPEED_ACC));
        } else {
            storedXSpeed = Math.Max(0, storedXSpeed - (Global.spf * SPEED_DEACC));
        }
        if (lastXDir != maverick.xDir && isHoldingDirection) {
            storedXSpeed = 0.25f;
        }

        if (!isHoldingDirection && storedXSpeed <= 30f) {
            maverick.changeState(new MIdle());
        }
        //Controller
        if (maverick.input.isPressed(Control.Jump, maverick.player)) {
            maverick.changeState(new MushroomJump(storedXSpeed, isDoubleJump: (maverick.grounded ? false : true)));
        }
        if (maverick.input.isPressed(Control.Down, maverick.player)) {
            maverick.changeState(new MushroomSpinDash(storedXSpeed * 1.2f));
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
    private bool hasEndedDoubleJumpAnim; //minor thing
    private int lastXDir;

    private const float SPEED_ACC = 180f;   //holding fowards
    private const float SPEED_DEACC = 230f; //not holding anything
    private const float SPEED_MAX = 200f;

    private const float JUMP_MOD = 1f;
    private const float JUMP_MOD2 = 1.2f;


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
            if (!player.input.isHeld(Control.Jump, player) || stateTime >= 0.4f) {
                if (!stoppedHoldingJump) {
                    stoppedHoldingJump = true;
                    maverick.vel.y *= 0.4f; //smoother
                    if (!isDoubleJump) {
                        maverick.changeSpriteFromName("fall", true);
                    }
                }
            }
            if (isDoubleJump && maverick.isAnimOver() && !hasEndedDoubleJumpAnim) {
                hasEndedDoubleJumpAnim = true;
                maverick.changeSpriteFromName("fall", true);
                maverick.frameIndex = 1;
            }
        }
        //double jump
        if (stateTime >= 0.02f && maverick.input.isPressed(Control.Jump, maverick.player) && !isDoubleJump) {
            // hardcoded moving to the left then jumping to the rigth
            if (maverick.input.isHeld(Control.Right, maverick.player) && maverick.xDir == 1 && storedXSpeed < 0) {
                maverick.changeState(new MushroomJump(storedXSpeed * -1, isDoubleJump: true));
            }
            // hardcoded moving to the right then jumping to the left
            else if (maverick.input.isHeld(Control.Left, maverick.player) && maverick.xDir == -1 && storedXSpeed < 0) {
                maverick.changeState(new MushroomJump(storedXSpeed * -1, isDoubleJump: true));
            } else {
                // if not jump normally.... papu wtF
                maverick.changeState(new MushroomJump(storedXSpeed, isDoubleJump: true));
            }
        }

        //end when grounded
        if (maverick.grounded && stateTime >= 0.1f) {
            //hardcoded canceling momentum if holding the opposite direction
            if (maverick.input.isHeld(Control.Right, maverick.player) && maverick.xDir == 1 && storedXSpeed < 0) {
                maverick.changeState(new MushroomRun(0));
            }
            //same here
            else if (maverick.input.isHeld(Control.Left, maverick.player) && maverick.xDir == -1 && storedXSpeed < 0) {
                maverick.changeState(new MushroomRun(0));
            } else {
                // if not jump normally.... papu wtF santos skibidis hardcoderos
                maverick.changeState(new MushroomRun(storedXSpeed));
            }
        }
        //change to wall
        var hitWall = Global.level.checkTerrainCollisionOnce(maverick, 1 * maverick.xDir, -2);
        if (hitWall?.isSideWallHit() == true &&
            (maverick.input.isHeld(Control.Left, maverick.player) || maverick.input.isHeld(Control.Right, maverick.player))) {
            maverick.xDir *= -1;
            maverick.changeState(new MushroomWall());
        }
    }
}
#endregion

#region ■ Wall ━━━━━━━
public class MushroomWall : MaverickState {
    public X4Mushroom minepe = null!;
    private bool isAttacking;
    private bool hasFiredLoop;

    public MushroomWall() : base("wall") {

    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.useGravity = false;
    }

    public override void update() {
        base.update();
        //is not attacking
        if (maverick.input.isPressed(Control.Shoot, maverick.player) && !isAttacking) {
            isAttacking = true;
            maverick.changeSpriteFromName("wall_attack", true);
        }
        if (isAttacking && !hasFiredLoop && maverick.frameIndex == 3) {
            hasFiredLoop = true;
            new MushroomBodyProj(maverick.pos.addxy(28 * maverick.xDir, 2), maverick.xDir, isFromWall: true, maverick, player, player.getNextActorNetId(), true);
        }
        if (isAttacking && maverick.frameIndex == 4) {
            hasFiredLoop = false;
        }
        if (!maverick.input.isHeld(Control.Left, maverick.player) && maverick.xDir == 1 || !maverick.input.isHeld(Control.Right, maverick.player) && maverick.xDir == -1) {
            maverick.changeState(new MushroomJump(0, false));
        }
        if (!maverick.input.isHeld(Control.Shoot, maverick.player) && isAttacking) {
            isAttacking = false;
            maverick.changeSpriteFromName("wall", true);
            maverick.frameIndex = 2;
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        maverick.useGravity = true;
    }
}
#endregion

#region ■ Croutch ━━━━━
public class MushroomCroutch : MaverickState { //aka spindash start
    public X4Mushroom minepe = null!;
    MushroomSpinProj? proj;
    private bool startedSpindashing;
    private float revLevel = 0;
    private float timeSinceLastRev = 1f;

    private const float MIN_LEVEL = 0;
    private const float MAX_LEVEL = 3;

    private readonly float[] levelFrameSpeeds = { 2f, 5f, 8f, 12f };
    private readonly float[] levelSpeeds = { 250f, 350f, 450f, 550f };

    private float levelSpeed = 0f;

    public MushroomCroutch() : base("croutch") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
        maverick.stopMoving();
    }

    public override void update() {
        base.update();
        levelSpeed = levelSpeeds[(int)revLevel];
        //------------------------------Croutch Handling------------------------
        // Initiate SpinDash/Croutch
        if (!input.isHeld(Control.Down, player)) {
            if (!startedSpindashing) {
                maverick.changeState(new MIdle());
            } else {
                maverick.changeState(new MushroomSpinDash(levelSpeed));
            }
        }
        //Initiate Revvin'
        if (input.isPressed(Control.Dash, player) && !startedSpindashing) {
            startedSpindashing = true;
            revLevel = 0;
            maverick.changeSpriteFromName("3dash_spin", true);
            proj = new MushroomSpinProj(maverick.pos, maverick.xDir, maverick, player, player.getNextActorNetId(), true);
        }

        //------------------------------Factor Handling------------------------

        //Decrease factor over time 
        if (startedSpindashing) {
            maverick.frameSpeed = levelFrameSpeeds[(int)revLevel]; //anim speed
            timeSinceLastRev -= Global.spf * 2f;   // 3.5 equals decrease time mult
        }
        if (timeSinceLastRev <= 0) {
            timeSinceLastRev = 1f;
            revLevel = Math.Max(MIN_LEVEL, revLevel - 1);
        }
        //Increase Factor by Control.Dash
        if (input.isPressed(Control.Dash, player) && startedSpindashing) {
            revLevel = Math.Min(MAX_LEVEL, revLevel + 1);
            timeSinceLastRev = 1f;

            new Anim(maverick.pos.addxy(5 * -maverick.xDir, 0), "dash_sparks",
            maverick.xDir, player.getNextActorNetId(), true, sendRpc: true);
        }
    }
    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        proj?.destroySelf();
    }
}
#endregion


#region ■ Spin Dash ━━━━
public class MushroomSpinDash : MaverickState { //Copypasted from Run, but removing a lot of
    public X4Mushroom minepe = null!;           //input holding mechanics not present in sanic
    MushroomSpinProj? proj;
    private float storedXSpeed;
    private float jumpTime;

    private bool isHoldingDirection;
    private bool stoppedHoldingJump;
    private bool shouldDeaccMore;
    private bool oldStateWasRun;
    private int lastXDir;

    private const float SPEED_DEACC = 160f; //not holding anything
    private const float SPEED_DEACC_HOLDING = 420f;  //holding opposite direction to stop
    private const float SPEED_MAX = 300f;


    public MushroomSpinDash(float storedXSpeed) : base("3dash_spin") {
        this.storedXSpeed = storedXSpeed;
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
        proj = new MushroomSpinProj(maverick.pos, maverick.xDir, maverick, player, player.getNextActorNetId(), true);
        if (oldState is MushroomRun) {
            oldStateWasRun = true;
        }
    }

    public override void preUpdate() {
        base.preUpdate();
        lastXDir = maverick.xDir;

    }

    public override void update() {
        base.update();

        //mimic stateTime for jump handling
        if (!maverick.grounded) {
            jumpTime += Global.spf;
        } else {
            jumpTime = 0;
        }

        maverick.move(new Point((int)(storedXSpeed * maverick.xDir), 0));

        if (maverick.xDir == -1 && maverick.input.isHeld(Control.Right, maverick.player) ||
            maverick.xDir == 1 && maverick.input.isHeld(Control.Left, maverick.player)) {
            shouldDeaccMore = true;
        }
        //flip when WALL hit
        var hitWall = Global.level.checkTerrainCollisionOnce(maverick, 1 * maverick.xDir, -2);
        if (hitWall?.isSideWallHit() == true) {
            maverick.xDir *= -1;
            float jumpXSpeed = Math.Abs(storedXSpeed) * 1;
            maverick.vel.y = -maverick.getJumpPower() * Math.Abs(storedXSpeed) * 0.002f;

        }
        //----------------------Movement Calculation------------------------------------
        //animation speed * math
        maverick.frameSpeed = Math.Max(1f, 1f * (storedXSpeed * 0.035f));
        storedXSpeed = Math.Max(0, storedXSpeed - (Global.spf * (shouldDeaccMore ? SPEED_DEACC_HOLDING : SPEED_DEACC)));

        if (maverick.grounded) {
            if (storedXSpeed <= 15f) {
                maverick.changeState(new MIdle());
            }
            if (storedXSpeed <= (shouldDeaccMore ? 60 : 240) && !oldStateWasRun) {
                if (maverick.input.isHeld(Control.Left, maverick.player) || maverick.input.isHeld(Control.Right, maverick.player)) {
                    maverick.changeState(new MushroomRun(storedXSpeed));
                }
            }
        }
        //----------------------Jump Controller------------------------------------
        if (maverick.input.isPressed(Control.Jump, maverick.player) && maverick.grounded) {
            maverick.vel.y = -maverick.getJumpPower() * 1.3f;
            jumpTime = 0;
            stoppedHoldingJump = false;
        }
        if (jumpTime >= 0.06f) { //buffer
            if (!player.input.isHeld(Control.Jump, player) || jumpTime >= 0.25f)
                if (!stoppedHoldingJump) {
                    stoppedHoldingJump = true;
                    maverick.vel.y *= 0.4f; //smoother
                    jumpTime = 0;
                }
        }
    }
    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        proj?.destroySelf();
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
public class MushroomHeadbutt : MaverickState {
    public X4Mushroom minepe = null!;
    bool hasLanded;

    public MushroomHeadbutt() : base("3dash_headbutt") {
    }

    public override void onEnter(MaverickState oldState) {
        base.onEnter(oldState);
        minepe = maverick as X4Mushroom ?? throw new NullReferenceException();
        maverick.stopMoving();
        maverick.gravityModifier = 0;
        maverick.frameSpeed = 0;
    }

    public override void update() {
        base.update();
        maverick.gravityModifier += Global.spf * 3f;
        if (maverick.grounded && !hasLanded) {
            maverick.shakeCamera();
            hasLanded = true;
            maverick.frameSpeed = 1;
            maverick.frameIndex = 1;

            new MushroomPoisonProj(maverick.pos.addxy(-10, -4), -1, 1, maverick, player, player.getNextActorNetId(), true);
            new MushroomPoisonProj(maverick.pos.addxy(-5, 0), -1, 0, maverick, player, player.getNextActorNetId(), true);
            new MushroomPoisonProj(maverick.pos.addxy(5, 0), 1, 0, maverick, player, player.getNextActorNetId(), true);
            new MushroomPoisonProj(maverick.pos.addxy(10, -4), 1, 1, maverick, player, player.getNextActorNetId(), true);

        }
        if (maverick.isAnimOver()) {
            maverick.changeState(new MIdle());
        }
    }

    public override void onExit(MaverickState newState) {
        base.onExit(newState);
        maverick.gravityModifier = 0.8f;
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
        return new MushrenzanProj(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
        if (isAnimOver()) destroySelf();
    }
    public override void postUpdate() {
        base.postUpdate(); this?.changePos(minepe.pos);
    }

    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion

#region ⬤ Mushroom Body ━
public class MushroomBodyProj : Projectile {
    public Actor? target;
    bool isFromWall;

    private bool hasFallen;
    private bool hasLanded;
    private bool hasIdled;
    private bool hasJumped;
    private bool hasSetWaitingSprite;
    private bool hasLockedTargetOnce;
    private bool hasSetDirectionToTarget;

    private int frameCount;
    private int jumpCounter;
    private float customAngle;

    private const float JUMP_MOD = 0.6f;
    private const float HOMING_SPEED = 400;
    private const float TIME_WAITING_FOR_TARGET = 1.2f;

    public MushroomBodyProj(
        Point pos, int xDir, bool isFromWall, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4mrm_1atk_body_start", netId, player
    ) {
        this.isFromWall = isFromWall;
        weapon = FakeZero.getWeapon();
        projId = (int)ProjIds.GBeetleGravityWell;
        vel = isFromWall ? vel = Point.zero : new Point((65 + new Random().Next(-25, 26)) * xDir, -240 + new Random().Next(-30, 31));
        maxTime = 4f;
        damager.damage = 1;
        //----------------------------//       
        frameSpeed = 0.8f;
        useGravity = isFromWall ? false : true;
        gravityModifier = 0.65f;
        destroyOnHit = true;
        destroyOnHitWall = false;
        fadeSprite = "mav_x4mrm_1atk_body_fade0";
        fadeOnAutoDestroy = true;
        xScale = isFromWall ? 1f : 0.6f;
        yScale = isFromWall ? 1f : 0.6f;

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new MushroomBodyProj(
            args.pos, args.xDir, true, args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
        frameCount++;
        globalCollider = new Collider(new Rect(0, 0, 25, 35).getPoints(),
        false, this, false, false, HitboxFlag.HitAndHurt, Point.zero); //isTrigger false first bool
        if (isFromWall && isAnimOver()) {
          useGravity = true; //fall when startsprite ends
        }
        //------------------------ Land, Hop twice then homing/destroySelf------------------------//
        if (vel.y >= 10 && !hasFallen) {
            hasFallen = true;
            changeSprite("mav_x4mrm_fall", true);
        }
        if (grounded && !hasLanded) {
            hasLanded = true;
            hasJumped = false; //jump reset
            vel = Point.zero;
            changeSprite("mav_x4mrm_land", true);
        }
        if (hasLanded && !hasIdled && isAnimOver()) {
            hasIdled = true;
            changeSprite("mav_x4mrm_idle", true);
        }
        if (hasIdled && !hasJumped && frameIndex > 0) {
            if (jumpCounter < 2) { //hop && reset hop bools
                hasJumped = true;
                jumpCounter++;
                hasFallen = false;
                hasLanded = false;
                hasIdled = false;
                changeSprite("mav_x4mrm_jump", true);
                vel.y = -Physics.JumpSpeed * JUMP_MOD;
                grounded = false;
            } else {
                //------------------------ Homing&Targeting Section ------------------------//
                //just visuals, set self destroy (wait time)
                if (!hasSetWaitingSprite) {
                    hasSetWaitingSprite = true;
                    changeSprite("mav_x4mrm_idle", true);
                    time = 0;
                    maxTime = TIME_WAITING_FOR_TARGET;
                }

                target = Global.level.getClosestTarget(pos, damager.owner.alliance, true, aMaxDist: 400);

                if (target != null) {
                    if (!hasLockedTargetOnce) {
                        hasLockedTargetOnce = true;
                        time = 0;
                        maxTime = 2.4f;
                        changeSprite("mav_x4mrm_3dash_spin", true);
                        frameSpeed = 24f;
                    }
                    if (loopCount > 5 && !hasSetDirectionToTarget) {
                        fadeSprite = "mav_x4mrm_1atk_body_fade1";
                        updateDamager(4, Global.defFlinch);

                        frameSpeed = 8f;
                        hasSetDirectionToTarget = true;
                        var dTo = pos.directionTo(target.getCenterPos()).normalize();
                        var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;
                        destAngle = Helpers.to360(destAngle);
                        vel.x = Helpers.cosd(destAngle) * HOMING_SPEED;
                        vel.y = Helpers.sind(destAngle) * HOMING_SPEED;
                    }
                }
            }
        }
    }

    public override void onHitWall(CollideData other) {
        base.onHitWall(other);
        if (other.isSideWallHit() && frameCount % 2 == 0) {
            vel.x *= -1;
        }
        if (hasSetDirectionToTarget && !other.isGroundHit()) destroySelf();
    }

    public override List<ShaderWrapper>? getShaders() {
        var shaders = new List<ShaderWrapper>();

        ShaderWrapper cloneShader = Helpers.cloneShaderSafe("soulBodyPalette");

        if (cloneShader != null) {
            int index = (frameCount / 2) % 7;
            if (index == 0) index++;

            cloneShader.SetUniform("palette", index);
            cloneShader.SetUniform("paletteTexture", Global.textures["soul_body_palette"]);
            shaders.Add(cloneShader);
        }
        if (shaders.Count > 0) {
            return shaders;
        } else {
            return base.getShaders();
        }
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
#region ⬤ Spin Proj ━━━
public class MushroomSpinProj : Projectile {
    public X4Mushroom minepe = null!;
    public MushroomSpinProj(
        Point pos, int xDir, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4mrm_3dash_spin_proj", netId, player
    ) {
        minepe = owner as X4Mushroom ?? throw new NullReferenceException();
        weapon = FakeZero.getWeapon();
        projId = (int)ProjIds.GBeetleGravityWell;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        vel = Point.zero;
        maxTime = 12f;
        destroyOnHit = false;
        destroyOnHitWall = false;

        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new MushroomSpinProj(
            args.pos, args.xDir, args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
        if (minepe != null) {
            this.frameSpeed = minepe.frameSpeed;
        }
    }
    public override void postUpdate() {
        base.postUpdate(); this?.changePos(minepe.pos);
    }

}
#endregion
#region ⬤ Poison Cloud ━━━
public class MushroomPoisonProj : Projectile {
    public MushroomPoisonProj(
        Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
    ) : base(
        pos, xDir, owner, "mav_x4mrm_3dash_poison_proj", netId, player
    ) {
        weapon = FakeZero.getWeapon();
        projId = (int)ProjIds.GBeetleGravityWell;
        vel = Point.zero;
        damager.damage = 1;
        damager.flinch = Global.defFlinch;
        damager.hitCooldown = 30;
        //----------------------------//       
        useGravity = true;
        gravityModifier = 0.18f;
        maxTime = 2f;
        destroyOnHit = false;
        destroyOnHitWall = false;
        switch (type) {
            case 0:
                vel = new Point(40 * xDir, -140);
                break;
            case 1:
                vel = new Point(80 * xDir, -120);
                break;
        }
        if (rpc) {
            rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
        return new MushroomPoisonProj(
            args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
        );
    }

    public override void update() {
        base.update();
        if (time > 0.24f) {
            vel.x = Helpers.lerp(vel.x, -50 * xDir, Global.spf * 1.5f);
        }
        if (isAnimOver()) destroySelf();
    }

    public override void onDestroy() {
        base.onDestroy();
    }
}
#endregion
#region ⬤ Bouncy Giga ━━━


#endregion



#endregion
