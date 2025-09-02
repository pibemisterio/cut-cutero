﻿using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class BaseSigma : Character {
	public const float sigmaHeight = 50;
	public float sigmaSaberMaxCooldown = 1f;
	public float noBlockTime = 0;
	public const float maxLeapSlashCooldown = 2;
	public float tagTeamSwapProgress;
	public int tagTeamSwapCase;
	public long lastAttackFrame = -100;
	public long framesSinceLastAttack = 1000;
	public bool isTrueAI;
	public bool tempAiSummoner;

	public SigmaLoadout loadout;
	public MaverickAIBehavior currentMaverickCommand;
	public bool summonerAttackModeActive;

	public BaseSigma(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true, SigmaLoadout? sigmaLoadout = null,
		int? heartTanks = null, bool isATrans = false
	) : base(
		player, x, y, xDir, isVisible,
		netId, ownedByLocalPlayer, isWarpIn, heartTanks, isATrans
	) {
		charId = CharIds.Sigma;
		// Special Sigma-only colider.
		spriteToCollider["head*"] = getSigmaHeadCollider();

		// To detect true AI (not used in F12 key)
		if (player.isAI && Global.level.mainPlayer != player) {
			isTrueAI = true;
		}
		// Configure weapons if local.
		sigmaLoadout ??= player.loadout.sigmaLoadout.clone();
		loadout = sigmaLoadout;
		weapons = configureWeapons(sigmaLoadout);
		if (ownedByLocalPlayer && weapons.Count == 3 && !isATrans) {
			weaponSlot = Options.main.sigmaWeaponSlot;
		}
		// For 1v1 mavericks.
		CharState intialCharState;
		if (ownedByLocalPlayer) {
			if (player.maverick1v1 != null) {
				intialCharState = new WarpOut(true);
			} else if (isWarpIn) {
				intialCharState = new WarpIn(true, true);
			} else {
				intialCharState = getIdleState();
			}
		} else {
			intialCharState = new NetLimbo();
			useGravity = false;
		}
		changeState(intialCharState);
	}

	public override int baselineMaxHealth() {
		return 20;
	}

	public Collider getSigmaHeadCollider() {
		var rect = new Rect(0, 0, 14, 20);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Point getDashDustEffectPos(int xDir) {
		float dashXPos = -35;
		return pos.addxy(dashXPos * xDir + (5 * xDir), -4);
	}

	public bool isSigmaShooting() {
		return sprite.name.Contains("_shoot_") || sprite.name.EndsWith("_shoot");
	}

	public override bool isSoftLocked() {
		if (currentMaverick != null) return true;
		return base.isSoftLocked();
	}

	public override void preUpdate() {
		base.preUpdate();

		if (!ownedByLocalPlayer) {
			return;
		}

		// To make F12 AIs into temporal summoners.
		if (player.isAI && !isTrueAI && !tempAiSummoner) {
			tempAiSummoner = true;
			foreach (Weapon weapon in weapons) {
				if (weapon is MaverickWeapon mw) {
					mw.controlMode = MaverickModeId.Summoner;
					if (mw.maverick != null && mw.trueControlMode != MaverickModeId.TagTeam) {
						mw.maverick.controlMode = MaverickModeId.Summoner;
					}
				}
			}
		}
		// Revert to their true control mode if was AI.
		else if (!player.isAI && !isTrueAI && tempAiSummoner) {
			tempAiSummoner = false;
			foreach (Weapon weapon in weapons) {
				if (weapon is MaverickWeapon mw) {
					mw.controlMode = mw.trueControlMode;
					if (mw.maverick != null) {
						if (mw.trueControlMode == MaverickModeId.TagTeam) {
							mw.maverick.changeState(new MExit(mw.maverick.pos, true), ignoreCooldown: true);
						} else {
							mw.maverick.controlMode = mw.trueControlMode;
						}
					}
				}
			}
		}

		bool isPuppeteer = false;
		bool isTruePuppeter = true;
		bool isTrueStriker = true;
		bool canIssueAttack = false;
		bool canIssueOrders = false;
		if (!player.isAI) {
			foreach (Weapon weapon in weapons) {
				if (weapon is not MaverickWeapon mw) {
					continue;
				}
				switch (mw.controlMode) {
					case MaverickModeId.Summoner: {
						canIssueAttack = true;
						canIssueOrders = true;
						isTruePuppeter = false;
						isTrueStriker = false;
						break;
					}
					case MaverickModeId.Puppeteer: {
						canIssueOrders = true;
						isPuppeteer = true;
						isTrueStriker = false;
						break;
					}
					case MaverickModeId.TagTeam: {
						isTruePuppeter = false;
						isTrueStriker = false;
						break;
					}
					case MaverickModeId.Striker: {
						isTruePuppeter = false;
						break;
					}
				}
			}
		}
		if (weapons.Count > 3 || isATrans) {
			isTruePuppeter = false;
			isTrueStriker = false;
		}

		if (isTruePuppeter && !player.isAI && Options.main.puppeteerHoldOrToggle &&
			!player.input.isHeld(Control.WeaponLeft, player) &&
			!player.input.isHeld(Control.WeaponRight, player)
		) {
			player.changeToSigmaSlot();
		}

		if (!isTrueStriker) {
			player.changeWeaponControls();
		}

		if (invulnTime > 0) return;

		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool spcPressed = player.input.isPressed(Control.Special1, player);
		if (flag != null) {
			shootPressed = false;
			spcPressed = false;
		}

		if (player.isAI && charState.attackCtrl && AI.trainingBehavior == AITrainingBehavior.Default) {
			foreach (Weapon weapon in weapons) {
				if (weapon is MaverickWeapon mw && mw.maverick == null) {
					if (mw.summonedOnce) {
						mw.summon(player, pos.addxy(0, -112), pos, xDir);
					} else if (canAffordMaverick(mw)) {
						buyMaverick(mw);
						mw.summon(player, pos.addxy(0, -112), pos, xDir);
						mw.summonedOnce = true;
					}
				}
			}
			return;
		}

		if (isPuppeteer) {
			if (currentWeapon is MaverickWeapon mw2 && mw2.maverick != null &&
				mw2.maverick.controlMode == MaverickModeId.Puppeteer
			) {
				if (mw2.maverick.aiBehavior != MaverickAIBehavior.Control && mw2.maverick.state is not MExit) {
					becomeMaverick(mw2.maverick);
				}
			} else if (currentMaverick != null) {
				Maverick? tagMaverick = null;
				if (charState is WarpOut) {
					foreach (Weapon weapon in weapons) {
						if (weapon is MaverickWeapon mw && mw.maverick?.controlMode == MaverickModeId.TagTeam) {
							tagMaverick = mw.maverick;
						}
					}
				}
				if (tagMaverick != null) {
					resetMaverickBehavior();
					tagMaverick.aiBehavior = MaverickAIBehavior.Control;
				} else {
					becomeSigma(pos, xDir);
				}
			}
		}

		// "Global" command prototype
		if (player.mavericks.Count > 0 &&
			grounded && player.input.isHeld(Control.Down, player) &&
			canIssueOrders && charState is not IssueGlobalCommand
		) {
			if (player.input.isCommandButtonPressed(player)) {
				Global.level.gameMode.hudErrorMsgTime = 0;
				if (currentMaverickCommand == MaverickAIBehavior.Defend) {
					currentMaverickCommand = MaverickAIBehavior.Follow;
					Global.level.gameMode.setHUDErrorMessage(player, "Issued follow command.", playSound: false);
				} else {
					currentMaverickCommand = MaverickAIBehavior.Defend;
					Global.level.gameMode.setHUDErrorMessage(player, "Issued hold position command.", playSound: false);
				}
				summonerAttackModeActive = false;

				foreach (var maverick in player.mavericks) {
					if (maverick != currentMaverick) {
						maverick.aiBehavior = currentMaverickCommand;
					}
				}
				if (charState is not WarpOut) {
					changeState(new IssueGlobalCommand(), true);
				}
			}
		} else if (
			player.mavericks.Count > 0 &&
			grounded && player.input.isHeld(Control.Up, player) &&
			canIssueOrders && charState is not IssueGlobalCommand
		) {
			if (player.input.isCommandButtonPressed(player)) {
				foreach (var maverick in player.mavericks) {
					if (maverick != currentMaverick && maverick.rootWeapon.canIssueOrders()) {
						maverick.changeState(new MExit(maverick.pos, true), ignoreCooldown: true);
					}
				}
				if (charState is not WarpOut) {
					changeState(new IssueGlobalCommand(), true);
				}
			}
		}

		if (player.mavericks.Count > 0 && grounded &&
			(player.input.isHeld(Control.Right, player) || player.input.isHeld(Control.Left, player))
			&& canIssueAttack && charState is not IssueGlobalCommand && charState is not Dash
		) {
			if (player.input.isCommandButtonPressed(player)) {
				Global.level.gameMode.hudErrorMsgTime = 0;

				summonerAttackModeActive = true;
				Global.level.gameMode.setHUDErrorMessage(player, "Issued attack-move command.", playSound: false);
				int attackDir = player.input.getXDir(player);
				if (attackDir == 0) {
					attackDir = currentMaverick?.xDir ?? xDir;
				}

				foreach (var maverick in player.mavericks) {
					if (maverick.rootWeapon.canIssueAttack()) {
						maverick.aiBehavior = MaverickAIBehavior.Attack;
						maverick.attackDir = attackDir;
					}
				}

				if (charState is not WarpOut) {
					changeState(new IssueGlobalCommand(), true);
				}
			}
		}
		// Target weapon.
		Weapon? targetWeapon = currentWeapon;
		// Striker controls.
		if (isTrueStriker && (
			player.input.isPressed(Control.WeaponLeft, player) ||
			player.input.isPressed(Control.WeaponRight, player)
		)) {
			Weapon[] mavericks = weapons.FindAll(w => w is MaverickWeapon).ToArray();
			if (mavericks.Length > 0) {
				if (player.input.isPressed(Control.WeaponRight, player) && mavericks.Length >= 2) {
					targetWeapon = mavericks[1];
				} else {
					targetWeapon = mavericks[0];
				}
			}
		}

		if (targetWeapon is MaverickWeapon mWeapon &&
			mWeapon.controlMode != MaverickModeId.TagTeam &&
			(mWeapon.cooldown == 0 || mWeapon.controlMode != MaverickModeId.Striker) &&
			(shootPressed || spcPressed || isTrueStriker && mWeapon.controlMode == MaverickModeId.Striker)
		) {
			if (mWeapon.maverick == null) {
				if (canAffordMaverick(mWeapon)) {
					if (!charState.attackCtrl) {
						return;
					}
					buyMaverick(mWeapon);
					Maverick maverick = mWeapon.summon(player, pos.addxy(0, -112), pos, xDir);
					if (mWeapon.controlMode == MaverickModeId.Striker) {
						Point inputDir = player.input.getInputDir(player);
						if (inputDir.y == -1) {
							maverick.startMoveControl = 1;
						} else if (inputDir.y == 1) {
							maverick.startMoveControl = 2;
						} else if (inputDir.x != -1) {
							maverick.startMoveControl = 3;
						} else {
							maverick.startMoveControl = 0;
						}
					} else {
						changeState(new CallDownMaverick(maverick, true, false), true);
					}
					if (mWeapon.controlMode == MaverickModeId.Striker) {
						maverick.aiCooldown = 30;
					}
					if (mWeapon.controlMode != MaverickModeId.Puppeteer) {
						player.changeToSigmaSlot();
					}
				} else {
					cantAffordMaverickMessage(mWeapon);
				}
			} else if (mWeapon.controlMode == MaverickModeId.Summoner) {
				if (shootPressed && mWeapon.shootCooldown == 0) {
					mWeapon.isMenuOpened = false;
					mWeapon.shootCooldown = MaverickWeapon.summonerCooldown;
					changeState(new CallDownMaverick(mWeapon.maverick, false, false), true);
					player.changeToSigmaSlot();
				}
			}
			return;
		}

		if (tagTeamSwapProgress == 0 &&
			shootPressed && (
				currentWeapon is MaverickWeapon ttmw &&
				ttmw.controlMode == MaverickModeId.TagTeam &&
				(ttmw.maverick == null || ttmw.maverick != currentMaverick)
			||
				currentMaverick?.controlMode == MaverickModeId.TagTeam &&
				currentWeapon is SigmaMenuWeapon
			)
		) {
			bool isMaverickIdle = currentMaverick?.state is MIdle mIdle;
			if (currentMaverick is MagnaCentipede ms && ms.reversedGravity) {
				isMaverickIdle = false;
			}
			bool isSigmaIdle = charState is Idle;

			if (isMaverickIdle && currentWeapon is SigmaMenuWeapon sw &&
				sw.shootCooldown == 0 && charState is not Die && tagTeamSwapProgress == 0
			) {
				tagTeamSwapProgress = 30;
				tagTeamSwapCase = 0;
			} else if (currentWeapon is MaverickWeapon mw &&
				mw.controlMode == MaverickModeId.TagTeam &&
				(mw.maverick == null || mw.maverick != currentMaverick) &&
				mw.cooldown == 0 && (isSigmaIdle || isMaverickIdle)
			) {
				if (canAffordMaverick(mw)) {
					tagTeamSwapProgress = 30;
					tagTeamSwapCase = 1;
				} else {
					cantAffordMaverickMessage(mw);
				}
			}
		}

		if (tagTeamSwapProgress > 0) {
			tagTeamSwapProgress -= speedMul;
			if (tagTeamSwapProgress <= 0) {
				tagTeamSwapProgress = 0;
				if (tagTeamSwapCase == 0) {
					var sw = weapons.FirstOrDefault(w => w is SigmaMenuWeapon);
					sw.shootCooldown = sw.fireRate;
					currentMaverick.changeState(new MExit(currentMaverick.pos, true));
					becomeSigma(currentMaverick.pos, currentMaverick.xDir);
				} else {
					if (currentWeapon is MaverickWeapon mw && mw.maverick == null) {
						buyMaverick(mw);

						Point currentPos = pos;
						if (currentMaverick == null) {
							changeState(new WarpOut());
						} else {
							currentPos = currentMaverick.pos;
							currentMaverick.changeState(new MExit(currentPos, true));
						}

						mw.summon(player, currentPos.addxy(0, -112), currentPos, xDir);
						mw.maverick!.health = mw.lastHealth;
						becomeMaverick(mw.maverick);
					}
				}
			}
		}
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		Helpers.decrementTime(ref noBlockTime);

		if (player.maverick1v1 != null && player.readyTextOver &&
			!player.maverick1v1Spawned && player.respawnTime <= 0 && weapons.Count > 0
		) {
			player.maverick1v1Spawned = true;
			var mw = weapons[0] as MaverickWeapon;
			if (mw != null) {
				mw.summon(player, pos.addxy(0, -112), pos, xDir);
				mw.maverick!.health = mw.lastHealth;
				becomeMaverick(mw.maverick);
			}
		}
		if (invulnTime > 0) {
			return;
		}
		if ((charState is Die || charState is WarpOut) && currentMaverick != null && !visible) {
			changePos(currentMaverick.pos);
		}
		if (charState is WarpOut) return;
		if (currentMaverick != null) {
			return;
		}
		if (currentWeapon is MaverickWeapon && (
			player.input.isHeld(Control.Shoot, player) ||
			player.input.isHeld(Control.Special1, player))
		) {
			return;
		}
		/*if (currentWeapon is MaverickWeapon mw2 && player.input.isPressed(Control.Special2, player)) {
			mw2.isMenuOpened = true;
		}*/
	}

	public override bool normalCtrl() {
		if (grounded && isControllingPuppet()) {
			changeState(new SigmaAutoBlock());
			return true;
		}
		bool changedState = base.normalCtrl();
		if (changedState || !charState.normalCtrl) {
			return true;
		}
		if (grounded && player.isCrouchHeld() && canGuard() &&
			!isAttacking() && noBlockTime == 0 &&
			charState is not SigmaBlock
		) {
			changeState(new SigmaBlock());
			return true;
		}
		return false;
	}

	public virtual bool canGuard() {
		if (isDashing) {
			return false;
		}
		return true;
	}

	public override bool canCrouch() {
		return false;
	}

	public void becomeSigma(Point pos, int xDir) {
		var prevCamPos = getCamCenterPos();

		if (currentMaverick?.controlMode != MaverickModeId.TagTeam && charState is not WarpOut) {
			resetMaverickBehavior();
			//stopCamUpdate = true;
			//Global.level.snapCamPos(getCamCenterPos());
			return;
		}
		resetMaverickBehavior();

		stopCamUpdate = true;
		Point raycastPos = pos.addxy(0, -5);
		Point? warpInPos = Global.level.getGroundPosNoKillzone(raycastPos, Global.screenH);

		if (warpInPos == null) {
			var nearestSpawnPoint = Global.level.getClosestSpawnPoint(pos);
			warpInPos = Global.level.getGroundPos(nearestSpawnPoint.pos);
		}

		changePos(warpInPos.Value);
		this.xDir = xDir;
		changeState(new WarpIn(false), true);
		Global.level.snapCamPos(getCamCenterPos(), prevCamPos);
	}

	public void becomeMaverick(Maverick maverick) {
		resetMaverickBehavior();
		maverick.aiBehavior = MaverickAIBehavior.Control;
		//stopCamUpdate = true;
		//Global.level.snapCamPos(getCamCenterPos());
		if (maverick.state is not MEnter && maverick.state is not MorphMHatchState && maverick.state is not MFly) {
			//To bring back puppeteer cancel, uncomment this
			if (Options.main.puppeteerCancel && maverick.state.canBeCanceled) {
				maverick.changeToIdleFallOrFly();
			}
		}
	}

	public void resetMaverickBehavior() {
		foreach (var weapon in weapons) {
			if (weapon is MaverickWeapon mw) {
				if (mw.maverick != null && mw.maverick.aiBehavior == MaverickAIBehavior.Control) {
					mw.maverick.aiBehavior = currentMaverickCommand;
					if (mw.maverick.controlMode == MaverickModeId.Summoner && summonerAttackModeActive) {
						mw.maverick.aiBehavior = MaverickAIBehavior.Attack;
					}
				}
				if (mw.isMenuOpened) {
					mw.isMenuOpened = false;
				}
			}
		}
	}

	public void buyMaverick(MaverickWeapon mw) {
		//if (Global.level.is1v1()) player.health -= (player.maxHealth / 2);
		if (mw.summonedOnce) return;
		int cost = getMaverickCost(mw);
		if (cost <= 0) {
			return;
		}
		player.currency -= cost;
	}

	private void cantAffordMaverickMessage(MaverickWeapon mw) {
		//if (Global.level.is1v1()) Global.level.gameMode.setHUDErrorMessage(player, "Maverick requires 16 HP");
		Global.level.gameMode.setHUDErrorMessage(
			player, "Maverick requires " + getMaverickCost(mw) + " " + Global.nameCoins
		);
	}

	public bool canAffordMaverick(MaverickWeapon mw) {
		//if (Global.level.is1v1()) return player.health > (player.maxHealth / 2);
		if (mw.summonedOnce) {
			return true;
		}
		int cost = getMaverickCost(mw);
		if (cost <= 0) {
			return true;
		}
		return player.currency >= cost;
	}

	public int getMaverickCost(MaverickWeapon mw) {
		// We cant expect true AI to do resource management.
		if (isTrueAI) {
			return 0;
		}
		// ATrans puppeter price.
		if (isATrans) {
			return mw.trueControlMode switch {
				MaverickModeId.Striker => 0,
				_ => 5
			};
		}
		// Regular prices for humans.
		return mw.trueControlMode switch {
			MaverickModeId.TagTeam => 0,
			MaverickModeId.Striker => 0,
			_ => 3
		};
	}

	public override bool canClimbLadder() {
		if (isSigmaShooting()) {
			return false;
		}
		return base.canClimbLadder();
	}

	public override Collider getGlobalCollider() {
		Rect rect = new Rect(0, 0, 18, BaseSigma.sigmaHeight);
		if (sprite.name.Contains("_ra_")) {
			rect.y2 = 20;
		}
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getTerrainCollider() {
		Collider? overrideGlobalCollider = null;
		if (spriteToColliderMatch(sprite.name, out overrideGlobalCollider)) {
			return overrideGlobalCollider;
		}
		if (physicsCollider == null) {
			return null;
		}
		float hSize = 40;
		if (sprite.name.Contains("crouch")) {
			hSize = 32;
		}
		if (sprite.name.Contains("dash")) {
			hSize = 32;
		}
		if (sprite.name.Contains("_ra_")) {
			hSize = 20;
		}
		return new Collider(
			new Rect(0f, 0f, 18, hSize).getPoints(),
			false, this, false, false,
			HitboxFlag.Hurtbox, new Point(0, 0)
		);
	}

	public override Collider getDashingCollider() {
		var rect = new Rect(0, 0, 18, 40);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getCrouchingCollider() {
		var rect = new Rect(0, 0, 18, 40);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getRaCollider() {
		var rect = new Rect(0, 0, 18, 30);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getRcCollider() {
		var rect = new Rect(0, -24, 18, 0);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	// This is not used... tecnically.
	public override Collider getBlockCollider() {
		var rect = new Rect(0, 0, 18, 34);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Point getCenterPos() {
		return pos.addxy(0, -32);
	}

	public override float getLabelOffY() {
		if (player.isMainPlayer && currentMaverick?.controlMode == MaverickModeId.TagTeam) {
			return currentMaverick.getLabelOffY();
		}
		if (sprite.name.Contains("_ra_")) {
			return 25;
		}
		return 62;
	}

	public override void render(float x, float y) {
		base.render(x, y);

		if (tagTeamSwapProgress > 0) {
			float healthBarInnerWidth = 30;

			float progress = 1 - (tagTeamSwapProgress / 30f);
			float width = progress * healthBarInnerWidth;

			getHealthNameOffsets(out bool shieldDrawn, ref progress);

			Point topLeft = new Point(pos.x - 16, pos.y - 5 + currentLabelY);
			Point botRight = new Point(pos.x + 16, pos.y + currentLabelY);

			DrawWrappers.DrawRect(
				topLeft.x, topLeft.y, botRight.x, botRight.y,
				true, Color.Black, 0, ZIndex.HUD - +1, outlineColor: Color.White
			);
			DrawWrappers.DrawRect(
				topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width, botRight.y - 1,
				true, Color.Yellow, 0, ZIndex.HUD + 1
			);

			Fonts.drawText(
				FontType.DarkGreen, "Swapping...", pos.x, pos.y - 15 + currentLabelY, Alignment.Center,
				true, depth: ZIndex.HUD
			);
			deductLabelY(labelCooldownOffY);
		}
	}

	public bool isAttacking() {
		if (isSigmaShooting() || sprite.name.Contains("attack")) {
			return true;
		}
		return false;
	}

	public override Point getCamCenterPos(bool ignoreZoom = false) {
		Maverick? maverick = currentMaverick;
		if (maverick != null && maverick.controlMode == MaverickModeId.TagTeam) {
			if (maverick.state is MEnter me) {
				return me.getDestPos().round().addxy(camOffsetX, -24);
			}
			if (maverick.state is MorphMCHangState hangState) {
				return maverick.pos.addxy(camOffsetX, -24 + 17);
			}
			return maverick.pos.round().addxy(camOffsetX, -24);
		}
		return base.getCamCenterPos(ignoreZoom);
	}

	public List<Weapon> configureWeapons(SigmaLoadout sigmaLoadout) {
		List<Weapon> retWeapons = [];
		if (Global.level.isTraining() && !Global.level.server.useLoadout) {
			retWeapons = Weapon.getAllSigmaWeapons(
				player, commandMode: sigmaLoadout.commandMode
			).Select(w => w.clone()).ToList();
		} else if (Global.level.is1v1()) {
			if (player.maverick1v1 != null) {
				retWeapons = [
					Weapon.getAllSigmaWeapons(
						player, sigmaLoadout.sigmaForm
					).Select(
						w => w.clone()
					).ToList()[player.maverick1v1.Value + 1]
				];
			} else if (!Global.level.isHyper1v1()) {
				int sigmaForm = sigmaLoadout.sigmaForm;
				retWeapons = Weapon.getAllSigmaWeapons(
					player, sigmaForm, sigmaLoadout.sigmaForm
				).Select(w => w.clone()).ToList();
			}
		}
		// Regular loadout.
		else {
			// Get command mode.
			int commandMode = sigmaLoadout.commandMode;
			// Always force AI into summoner.
			if (isTrueAI) {
				commandMode = (int)MaverickModeId.Summoner;
			}
			// Get weapons.
			if (!oldATrans) {
				retWeapons = [
					SigmaLoadout.getWeaponById(player, sigmaLoadout.maverick1, sigmaLoadout.commandMode),
					SigmaLoadout.getWeaponById(player, sigmaLoadout.maverick2, sigmaLoadout.commandMode)
				];
			}
			// Push the generic Sigma slot.
			int sigmaWeaponSlot = 1;
			// Always put the AI and enemies slot in the center.
			if (Global.level.mainPlayer == player) {
				sigmaWeaponSlot = Helpers.clamp(Options.main.sigmaWeaponSlot, 0, 2);
			}
			retWeapons.Insert(sigmaWeaponSlot, new SigmaMenuWeapon());
			weaponSlot = sigmaWeaponSlot;
		}
		// Preserve HP on death so can summon for free until they die.
		if (!isATrans &&
			player.oldWeapons != null &&
			sigmaLoadout.commandMode != (int)MaverickModeId.Striker &&
			player.previousLoadout?.sigmaLoadout.commandMode == sigmaLoadout.commandMode
		) {
			foreach (var weapon in retWeapons) {
				if (weapon is not MaverickWeapon mw) {
					continue;
				}
				if (player.oldWeapons.FirstOrDefault(
						w => w is MaverickWeapon && w.GetType() == weapon.GetType()
					) is not MaverickWeapon matchingOldWeapon
				) {
					continue;
				}
				if (matchingOldWeapon.lastHealth > 0 && matchingOldWeapon.summonedOnce) {
					mw.summonedOnce = true;
					mw.lastHealth = matchingOldWeapon.lastHealth;
					mw.isMoth = matchingOldWeapon.isMoth;
				}
			}
		}
		return retWeapons;
	}
	public override void onDeath() {
		base.onDeath();
		foreach (var maverick in player.mavericks) {
			if (maverick != null) {
				maverick.autoExit = true;
			}
		}
	}
}
