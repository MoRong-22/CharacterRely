/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated May 1, 2019. Replaces all prior versions.
 *
 * Copyright (c) 2013-2019, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software
 * or otherwise create derivative works of the Spine Runtimes (collectively,
 * "Products"), provided that each user of the Products must obtain their own
 * Spine Editor license and redistribution of the Products in any form must
 * include this license and copyright notice.
 *
 * THIS SOFTWARE IS PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY EXPRESS
 * OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN
 * NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES, BUSINESS
 * INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

using System;

namespace Spine {
	/// <summary>骨骼运算使用的数学常量、三角函数查表和插值工具。</summary>
	public static class MathUtils {
		/// <summary>圆周率 π。</summary>
		public const float PI = 3.1415927f;
		/// <summary>完整一圈的弧度值 2π。</summary>
		public const float PI2 = PI * 2;
		/// <summary>弧度转换为角度的乘数 180/π。</summary>
		public const float RadDeg = 180f / PI;
		/// <summary>角度转换为弧度的乘数 π/180。</summary>
		public const float DegRad = PI / 180;

		/// <summary>正弦查找表的索引位数，用于决定查表精度。</summary>
		const int SIN_BITS = 14; // 查找表约占 16KB，可调整表大小以平衡精度和性能。 原注： 16KB. Adjust for accuracy.
		/// <summary>正弦查找表索引循环取模的位掩码。</summary>
		const int SIN_MASK = ~(-1 << SIN_BITS);
		/// <summary>正弦查找表的元素数量。</summary>
		const int SIN_COUNT = SIN_MASK + 1;
		/// <summary>完整一圈的弧度值。</summary>
		const float RadFull = PI * 2;
		/// <summary>完整一圈的角度值。</summary>
		const float DegFull = 360;
		/// <summary>弧度换算为三角函数查表索引的倍率。</summary>
		const float RadToIndex = SIN_COUNT / RadFull;
		/// <summary>角度换算为三角函数查表索引的倍率。</summary>
		const float DegToIndex = SIN_COUNT / DegFull;
		/// <summary>预计算的正弦值查找表。</summary>
		static float[] sin = new float[SIN_COUNT];

		/// <summary>生成运行时随机值的随机数源。</summary>
		static Random random = new Random();

		/// <summary>预计算正弦查找表，并修正常见直角的精确值。</summary>
		static MathUtils () {
			for (int i = 0; i < SIN_COUNT; i++)
				sin[i] = (float)Math.Sin((i + 0.5f) / SIN_COUNT * RadFull);
			for (int i = 0; i < 360; i += 90)
				sin[(int)(i * DegToIndex) & SIN_MASK] = (float)Math.Sin(i * DegRad);
		}

		/// <summary><para>通过查找表求正弦，输入角度单位为弧度。</para>Returns the sine in radians from a lookup table.</summary>
		static public float Sin (float radians) {
			return sin[(int)(radians * RadToIndex) & SIN_MASK];
		}

		/// <summary><para>通过查找表求余弦，输入角度单位为弧度。</para>Returns the cosine in radians from a lookup table.</summary>
		static public float Cos (float radians) {
			return sin[(int)((radians + PI / 2) * RadToIndex) & SIN_MASK];
		}

		/// <summary><para>通过查找表求正弦，此方法输入单位为角度（度）。</para>Returns the sine in radians from a lookup table.</summary>
		static public float SinDeg (float degrees) {
			return sin[(int)(degrees * DegToIndex) & SIN_MASK];
		}

		/// <summary><para>通过查找表求余弦，此方法输入单位为角度（度）。</para>Returns the cosine in radians from a lookup table.</summary>
		static public float CosDeg (float degrees) {
			return sin[(int)((degrees + 90) * DegToIndex) & SIN_MASK];
		}

		/// <summary><para>近似计算 atan2，结果为弧度；比 Math.Atan2 快但精度较低，最大误差约 0.00488 弧度。</para>Returns atan2 in radians, faster but less accurate than Math.Atan2. Average error of 0.00231 radians (0.1323
		/// degrees), largest error of 0.00488 radians (0.2796 degrees).</summary>
		static public float Atan2 (float y, float x) {
			if (x == 0f) {
				if (y > 0f) return PI / 2;
				if (y == 0f) return 0f;
				return -PI / 2;
			}
			float atan, z = y / x;
			if (Math.Abs(z) < 1f) {
				atan = z / (1f + 0.28f * z * z);
				if (x < 0f) return atan + (y < 0f ? -PI : PI);
				return atan;
			}
			atan = PI / 2 - z / (z * z + 0.28f);
			return y < 0f ? atan - PI : atan;
		}

		/// <summary>将数值限制在指定最小值和最大值之间。</summary>
		static public float Clamp (float value, float min, float max) {
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}

		/// <summary>生成指定范围内的三角分布随机值，mode 为概率最高的位置。</summary>
		static public float RandomTriangle(float min, float max) {
			return RandomTriangle(min, max, (min + max) * 0.5f);
		}

		/// <summary>生成指定范围内的三角分布随机值，mode 为概率最高的位置。</summary>
		static public float RandomTriangle(float min, float max, float mode) {
			float u = (float)random.NextDouble();
			float d = max - min;
			if (u <= (mode - min) / d) return min + (float)Math.Sqrt(u * d * (mode - min));
			return max - (float)Math.Sqrt((1 - u) * d * (max - mode));
		}
	}

	/// <summary>将归一化进度转换为缓动比例的插值基类。</summary>
	public abstract class IInterpolation {
		/// <summary>平方曲线缓入缓出插值实例。</summary>
		public static IInterpolation Pow2 = new Pow(2);
		/// <summary>平方曲线缓出插值实例。</summary>
		public static IInterpolation Pow2Out = new PowOut(2);

		/// <summary>将归一化进度转换为幂函数缓动比例。</summary>
		protected abstract float Apply(float a);

		/// <summary>使用缓动后的比例在起点和终点之间插值。</summary>
		public float Apply(float start, float end, float a) {
			return start + (end - start) * Apply(a);
		}
	}

	/// <summary>幂函数对称缓入缓出插值。</summary>
	public class Pow: IInterpolation {
		/// <summary>幂函数指数。</summary>
		public float Power { get; set; }

		/// <summary>创建幂函数对称缓入缓出插值，并初始化关联数据及缓冲区。</summary>
		public Pow(float power) {
			Power = power;
		}

		/// <summary>将归一化进度转换为幂函数缓动比例。</summary>
		protected override float Apply(float a) {
			if (a <= 0.5f) return (float)Math.Pow(a * 2, Power) / 2;
			return (float)Math.Pow((a - 1) * 2, Power) / (Power % 2 == 0 ? -2 : 2) + 1;
		}
	}

	/// <summary>幂函数缓出插值。</summary>
	public class PowOut : Pow {
		/// <summary>创建幂函数缓出插值，并初始化关联数据及缓冲区。</summary>
		public PowOut(float power) : base(power) {
		}

		/// <summary>将归一化进度转换为幂函数缓动比例。</summary>
		protected override float Apply(float a) {
			return (float)Math.Pow(a - 1, Power) * (Power % 2 == 0 ? -1 : 1) + 1;
		}
	}
}
