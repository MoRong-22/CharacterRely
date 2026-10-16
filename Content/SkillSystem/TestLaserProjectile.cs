using System;
using CharacterRely.Content.ModCharacter;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CharacterRely.Content.SkillSystem;

/// <summary>从手掌发出的持续激光。绘制与伤害共用同一条被物块截断的线段。</summary>
public sealed class TestLaserProjectile : ModProjectile
{
    /// <summary>最大射程，单位为像素。</summary>
    public const float MaxLength = 700f;
    /// <summary>光束本体与碰撞宽度。</summary>
    public const float BeamWidth = 18f;
    /// <summary>物块扫描复用缓冲区，避免每帧分配。</summary>
    private readonly float[] scan = new float[3];
    /// <summary>本帧有效长度、粗细和起点同步间隔。</summary>
    private float beamLength, strength;
    private int syncTicks;

    /// <summary>使用原版白点绘制，不需要新贴图或着色器。</summary>
    public override string Texture => "Terraria/Images/MagicPixel";

    /// <summary>扩大屏幕外检查范围，使长光束仍可见。</summary>
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)MaxLength;

    /// <summary>可连续命中不同敌人，同一敌人每 12 帧最多受击一次。</summary>
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 6;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.penetrate = -1;
        Projectile.timeLeft = TestLaserSkill.BeamTicks;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 12;
        Projectile.netImportant = true;
    }

    /// <summary>velocity 存放光束方向，不让弹幕自动沿它移动。</summary>
    public override bool ShouldUpdatePosition() => false;
    /// <summary>光束不破坏草、罐子等物块。</summary>
    public override bool? CanCutTiles() => false;

    /// <summary>跟随手掌，原生弹幕同步携带方向和相对玩家的发射点。</summary>
    public override void AI()
    {
        if ((uint)Projectile.owner >= (uint)Main.maxPlayers) { Projectile.Kill(); return; }
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead) { Projectile.Kill(); return; }
        if (Projectile.owner == Main.myPlayer)
        {
            var skill = player.GetModPlayer<TestCharacter>().LaserSkill;
            if (!skill.IsActive || !skill.TryGetOrigin(out Vector2 hand)) { Projectile.Kill(); return; }
            Vector2 offset = hand - player.Center;
            Projectile.ai[0] = offset.X;
            Projectile.ai[1] = offset.Y;
            if (++syncTicks >= 6) { syncTicks = 0; Projectile.netUpdate = true; }
        }
        Projectile.Center = player.Center + new Vector2(Projectile.ai[0], Projectile.ai[1]);
        Projectile.velocity = Projectile.velocity.SafeNormalize(new Vector2(player.direction, 0f));
        int age = TestLaserSkill.BeamTicks - Projectile.timeLeft;
        strength = MathHelper.Clamp(MathF.Min((age + 1) / 3f, Projectile.timeLeft / 6f), 0f, 1f);
        // 手掌如果已经伸到墙另一侧，也不允许光束从墙后开始伤害。
        if (!Collision.CanHitLine(player.Center, 1, 1, Projectile.Center, 1, 1)) { beamLength = 0f; return; }
        Collision.LaserScan(Projectile.Center, Projectile.velocity, BeamWidth * strength, MaxLength, scan);
        beamLength = MathF.Min(scan[0], MathF.Min(scan[1], scan[2]));
    }

    /// <summary>淡出后或被完全挡住时不造成伤害。</summary>
    public override bool? CanDamage() => strength > 0f && beamLength > 0f ? null : false;

    /// <summary>用实际光束宽度检测线段碰撞，远处敌人也能正确受击。</summary>
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float collisionPoint = 0f;
        return beamLength > 0f && Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center, Projectile.Center + Projectile.velocity * beamLength,
            BeamWidth * strength, ref collisionPoint);
    }

    /// <summary>青蓝色外层、浅色主体与白色核心；不改 SpriteBatch 状态。</summary>
    public override bool PreDraw(ref Color lightColor)
    {
        if (beamLength <= 0f) return false;
        DrawBeam(BeamWidth * 1.4f, new Color(35, 130, 255) * 0.3f);
        DrawBeam(BeamWidth, new Color(80, 220, 255));
        DrawBeam(BeamWidth * 0.4f, Color.White);
        return false;
    }

    /// <summary>按同一长度绘制一层光束，左右方向直接由单位向量决定。</summary>
    private void DrawBeam(float width, Color color)
        => Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, Projectile.Center - Main.screenPosition,
            null, color * strength, Projectile.velocity.ToRotation(), new Vector2(0f, 0.5f),
            new Vector2(beamLength, width * strength), SpriteEffects.None);
}
