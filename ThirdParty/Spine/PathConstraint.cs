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

	/// <summary><para>路径约束当前状态；调整骨骼旋转、位移或缩放，使其跟随路径附件。</para>
	/// <para>
	/// Stores the current pose for a path constraint. A path constraint adjusts the rotation, translation, and scale of the
	/// constrained bones so they follow a {@link PathAttachment}.</para>
	/// <para>
	/// See <a href="http://esotericsoftware.com/spine-path-constraints">Path constraints</a> in the Spine User Guide.</para>
	/// </summary>
	public class PathConstraint : IUpdatable {
		/// <summary>路径采样内部标记：未选择曲线、起点之前、终点之后。</summary>
		const int NONE = -1, BEFORE = -2, AFTER = -3;
		/// <summary>比较浮点数及避免零除的微小量。</summary>
		const float Epsilon = 0.00001f;

		/// <summary>初始配置数据。</summary>
		internal PathConstraintData data;
		/// <summary>关联骨骼集合。</summary>
		internal ExposedList<Bone> bones;
		/// <summary>约束目标。</summary>
		internal Slot target;
		/// <summary>position：沿路径的位置；spacing：沿路径的骨骼间距；rotateMix：旋转混合权重；translateMix：位移混合权重。</summary>
		internal float position, spacing, rotateMix, translateMix;

		/// <summary>是否参与当前骨架更新。</summary>
		internal bool active;

		/// <summary>spaces：路径采样间距缓存；positions：路径采样位置及方向缓存。</summary>
		internal ExposedList<float> spaces = new ExposedList<float>(), positions = new ExposedList<float>();
		/// <summary>world：路径顶点世界坐标缓存；curves：曲线累计长度或插值记录缓存；lengths：骨骼或曲线长度缓存。</summary>
		internal ExposedList<float> world = new ExposedList<float>(), curves = new ExposedList<float>(), lengths = new ExposedList<float>();
		/// <summary>曲线细分段长度缓存。</summary>
		internal float[] segments = new float[10];

		/// <summary>创建让骨骼跟随路径附件的约束，并初始化关联数据及缓冲区。</summary>
		public PathConstraint (PathConstraintData data, Skeleton skeleton) {
			if (data == null) throw new ArgumentNullException("data", "data cannot be null.");
			if (skeleton == null) throw new ArgumentNullException("skeleton", "skeleton cannot be null.");
			this.data = data;
			bones = new ExposedList<Bone>(data.Bones.Count);
			foreach (BoneData boneData in data.bones)
				bones.Add(skeleton.FindBone(boneData.name));
			target = skeleton.FindSlot(data.target.name);
			position = data.position;
			spacing = data.spacing;
			rotateMix = data.rotateMix;
			translateMix = data.translateMix;
		}

		/// <summary><para>复制构造函数。</para>Copy constructor.</summary>
		public PathConstraint (PathConstraint constraint, Skeleton skeleton) {
			if (constraint == null) throw new ArgumentNullException("constraint cannot be null.");
			if (skeleton == null) throw new ArgumentNullException("skeleton cannot be null.");
			data = constraint.data;
			bones = new ExposedList<Bone>(constraint.Bones.Count);
			foreach (Bone bone in constraint.Bones)
				bones.Add(skeleton.Bones.Items[bone.data.index]);
			target = skeleton.slots.Items[constraint.target.data.index];
			position = constraint.position;
			spacing = constraint.spacing;
			rotateMix = constraint.rotateMix;
			translateMix = constraint.translateMix;
		}

		/// <summary><para>将约束应用到受约束的骨骼。</para>Applies the constraint to the constrained bones.</summary>
		public void Apply () {
			Update();
		}

		/// <summary>按当前参数计算约束，并更新受影响骨骼的世界姿势。</summary>
		public void Update () {
			PathAttachment attachment = target.Attachment as PathAttachment;
			if (attachment == null) return;

			float rotateMix = this.rotateMix, translateMix = this.translateMix;
			bool translate = translateMix > 0, rotate = rotateMix > 0;
			if (!translate && !rotate) return;

			PathConstraintData data = this.data;
			bool percentSpacing = data.spacingMode == SpacingMode.Percent;
			RotateMode rotateMode = data.rotateMode;
			bool tangents = rotateMode == RotateMode.Tangent, scale = rotateMode == RotateMode.ChainScale;
			int boneCount = this.bones.Count, spacesCount = tangents ? boneCount : boneCount + 1;
			Bone[] bonesItems = this.bones.Items;
			ExposedList<float> spaces = this.spaces.Resize(spacesCount), lengths = null;
			float spacing = this.spacing;
			if (scale || !percentSpacing) {
				if (scale) lengths = this.lengths.Resize(boneCount);
				bool lengthSpacing = data.spacingMode == SpacingMode.Length;
				for (int i = 0, n = spacesCount - 1; i < n;) {
					Bone bone = bonesItems[i];
					float setupLength = bone.data.length;
					if (setupLength < PathConstraint.Epsilon) {
						if (scale) lengths.Items[i] = 0;
						spaces.Items[++i] = 0;
					} else if (percentSpacing) {
						if (scale) {
							float x = setupLength * bone.a, y = setupLength * bone.c;
							float length = (float)Math.Sqrt(x * x + y * y);
							lengths.Items[i] = length;
						}
						spaces.Items[++i] = spacing;
					} else {
						float x = setupLength * bone.a, y = setupLength * bone.c;
						float length = (float)Math.Sqrt(x * x + y * y);
						if (scale) lengths.Items[i] = length;
						spaces.Items[++i] = (lengthSpacing ? setupLength + spacing : spacing) * length / setupLength;
					}
				}
			} else {
				for (int i = 1; i < spacesCount; i++)
					spaces.Items[i] = spacing;
			}

			float[] positions = ComputeWorldPositions(attachment, spacesCount, tangents,
				data.positionMode == PositionMode.Percent, percentSpacing);
			float boneX = positions[0], boneY = positions[1], offsetRotation = data.offsetRotation;
			bool tip;
			if (offsetRotation == 0) {
				tip = rotateMode == RotateMode.Chain;
			} else {
				tip = false;
				Bone p = target.bone;
				offsetRotation *= p.a * p.d - p.b * p.c > 0 ? MathUtils.DegRad : -MathUtils.DegRad;
			}
			for (int i = 0, p = 3; i < boneCount; i++, p += 3) {
				Bone bone = bonesItems[i];
				bone.worldX += (boneX - bone.worldX) * translateMix;
				bone.worldY += (boneY - bone.worldY) * translateMix;
				float x = positions[p], y = positions[p + 1], dx = x - boneX, dy = y - boneY;
				if (scale) {
					float length = lengths.Items[i];
					if (length >= PathConstraint.Epsilon) {
						float s = ((float)Math.Sqrt(dx * dx + dy * dy) / length - 1) * rotateMix + 1;
						bone.a *= s;
						bone.c *= s;
					}
				}
				boneX = x;
				boneY = y;
				if (rotate) {
					float a = bone.a, b = bone.b, c = bone.c, d = bone.d, r, cos, sin;
					if (tangents)
						r = positions[p - 1];
					else if (spaces.Items[i + 1] < PathConstraint.Epsilon)
						r = positions[p + 2];
					else
						r = MathUtils.Atan2(dy, dx);
					r -= MathUtils.Atan2(c, a);
					if (tip) {
						cos = MathUtils.Cos(r);
						sin = MathUtils.Sin(r);
						float length = bone.data.length;
						boneX += (length * (cos * a - sin * c) - dx) * rotateMix;
						boneY += (length * (sin * a + cos * c) - dy) * rotateMix;
					} else
						r += offsetRotation;
					if (r > MathUtils.PI)
						r -= MathUtils.PI2;
					else if (r < -MathUtils.PI) //
						r += MathUtils.PI2;
					r *= rotateMix;
					cos = MathUtils.Cos(r);
					sin = MathUtils.Sin(r);
					bone.a = cos * a - sin * c;
					bone.b = cos * b - sin * d;
					bone.c = sin * a + cos * c;
					bone.d = sin * b + cos * d;
				}
				bone.appliedValid = false;
			}
		}

		/// <summary>按路径位置和间距采样世界坐标，可同时计算切线方向。</summary>
		float[] ComputeWorldPositions (PathAttachment path, int spacesCount, bool tangents, bool percentPosition,
			bool percentSpacing) {

			Slot target = this.target;
			float position = this.position;
			float[] spacesItems = this.spaces.Items, output = this.positions.Resize(spacesCount * 3 + 2).Items, world;
			bool closed = path.Closed;
			int verticesLength = path.WorldVerticesLength, curveCount = verticesLength / 6, prevCurve = NONE;
			float pathLength = 0;

			if (!path.ConstantSpeed) {
				float[] lengths = path.Lengths;
				curveCount -= closed ? 1 : 2;
				pathLength = lengths[curveCount];
				if (percentPosition) position *= pathLength;
				if (percentSpacing) {
					for (int i = 1; i < spacesCount; i++)
						spacesItems[i] *= pathLength;
				}
				world = this.world.Resize(8).Items;
				for (int i = 0, o = 0, curve = 0; i < spacesCount; i++, o += 3) {
					float space = spacesItems[i];
					position += space;
					float p = position;

					if (closed) {
						p %= pathLength;
						if (p < 0) p += pathLength;
						curve = 0;
					} else if (p < 0) {
						if (prevCurve != BEFORE) {
							prevCurve = BEFORE;
							path.ComputeWorldVertices(target, 2, 4, world, 0, 2);
						}
						AddBeforePosition(p, world, 0, output, o);
						continue;
					} else if (p > pathLength) {
						if (prevCurve != AFTER) {
							prevCurve = AFTER;
							path.ComputeWorldVertices(target, verticesLength - 6, 4, world, 0, 2);
						}
						AddAfterPosition(p - pathLength, world, 0, output, o);
						continue;
					}

					// 确定当前位置位于哪一段曲线。 原注： Determine curve containing position.
					for (;; curve++) {
						float length = lengths[curve];
						if (p > length) continue;
						if (curve == 0)
							p /= length;
						else {
							float prev = lengths[curve - 1];
							p = (p - prev) / (length - prev);
						}
						break;
					}
					if (curve != prevCurve) {
						prevCurve = curve;
						if (closed && curve == curveCount) {
							path.ComputeWorldVertices(target, verticesLength - 4, 4, world, 0, 2);
							path.ComputeWorldVertices(target, 0, 4, world, 4, 2);
						} else
							path.ComputeWorldVertices(target, curve * 6 + 2, 8, world, 0, 2);
					}
					AddCurvePosition(p, world[0], world[1], world[2], world[3], world[4], world[5], world[6], world[7], output, o,
						tangents || (i > 0 && space < PathConstraint.Epsilon));
				}
				return output;
			}

			// 世界坐标顶点。 原注： World vertices.
			if (closed) {
				verticesLength += 2;
				world = this.world.Resize(verticesLength).Items;
				path.ComputeWorldVertices(target, 2, verticesLength - 4, world, 0, 2);
				path.ComputeWorldVertices(target, 0, 2, world, verticesLength - 4, 2);
				world[verticesLength - 2] = world[0];
				world[verticesLength - 1] = world[1];
			} else {
				curveCount--;
				verticesLength -= 4;
				world = this.world.Resize(verticesLength).Items;
				path.ComputeWorldVertices(target, 2, verticesLength, world, 0, 2);
			}

			// 各段曲线的长度。 原注： Curve lengths.
			float[] curves = this.curves.Resize(curveCount).Items;
			pathLength = 0;
			float x1 = world[0], y1 = world[1], cx1 = 0, cy1 = 0, cx2 = 0, cy2 = 0, x2 = 0, y2 = 0;
			float tmpx, tmpy, dddfx, dddfy, ddfx, ddfy, dfx, dfy;
			for (int i = 0, w = 2; i < curveCount; i++, w += 6) {
				cx1 = world[w];
				cy1 = world[w + 1];
				cx2 = world[w + 2];
				cy2 = world[w + 3];
				x2 = world[w + 4];
				y2 = world[w + 5];
				tmpx = (x1 - cx1 * 2 + cx2) * 0.1875f;
				tmpy = (y1 - cy1 * 2 + cy2) * 0.1875f;
				dddfx = ((cx1 - cx2) * 3 - x1 + x2) * 0.09375f;
				dddfy = ((cy1 - cy2) * 3 - y1 + y2) * 0.09375f;
				ddfx = tmpx * 2 + dddfx;
				ddfy = tmpy * 2 + dddfy;
				dfx = (cx1 - x1) * 0.75f + tmpx + dddfx * 0.16666667f;
				dfy = (cy1 - y1) * 0.75f + tmpy + dddfy * 0.16666667f;
				pathLength += (float)Math.Sqrt(dfx * dfx + dfy * dfy);
				dfx += ddfx;
				dfy += ddfy;
				ddfx += dddfx;
				ddfy += dddfy;
				pathLength += (float)Math.Sqrt(dfx * dfx + dfy * dfy);
				dfx += ddfx;
				dfy += ddfy;
				pathLength += (float)Math.Sqrt(dfx * dfx + dfy * dfy);
				dfx += ddfx + dddfx;
				dfy += ddfy + dddfy;
				pathLength += (float)Math.Sqrt(dfx * dfx + dfy * dfy);
				curves[i] = pathLength;
				x1 = x2;
				y1 = y2;
			}
			if (percentPosition)
				position *= pathLength;
			else
				position *= pathLength / path.lengths[curveCount - 1];

			if (percentSpacing) {
				for (int i = 1; i < spacesCount; i++)
					spacesItems[i] *= pathLength;
			}

			float[] segments = this.segments;
			float curveLength = 0;
			for (int i = 0, o = 0, curve = 0, segment = 0; i < spacesCount; i++, o += 3) {
				float space = spacesItems[i];
				position += space;
				float p = position;

				if (closed) {
					p %= pathLength;
					if (p < 0) p += pathLength;
					curve = 0;
				} else if (p < 0) {
					AddBeforePosition(p, world, 0, output, o);
					continue;
				} else if (p > pathLength) {
					AddAfterPosition(p - pathLength, world, verticesLength - 4, output, o);
					continue;
				}

				// 确定当前位置位于哪一段曲线。 原注： Determine curve containing position.
				for (;; curve++) {
					float length = curves[curve];
					if (p > length) continue;
					if (curve == 0)
						p /= length;
					else {
						float prev = curves[curve - 1];
						p = (p - prev) / (length - prev);
					}
					break;
				}

				// 曲线细分段的长度。 原注： Curve segment lengths.
				if (curve != prevCurve) {
					prevCurve = curve;
					int ii = curve * 6;
					x1 = world[ii];
					y1 = world[ii + 1];
					cx1 = world[ii + 2];
					cy1 = world[ii + 3];
					cx2 = world[ii + 4];
					cy2 = world[ii + 5];
					x2 = world[ii + 6];
					y2 = world[ii + 7];
					tmpx = (x1 - cx1 * 2 + cx2) * 0.03f;
					tmpy = (y1 - cy1 * 2 + cy2) * 0.03f;
					dddfx = ((cx1 - cx2) * 3 - x1 + x2) * 0.006f;
					dddfy = ((cy1 - cy2) * 3 - y1 + y2) * 0.006f;
					ddfx = tmpx * 2 + dddfx;
					ddfy = tmpy * 2 + dddfy;
					dfx = (cx1 - x1) * 0.3f + tmpx + dddfx * 0.16666667f;
					dfy = (cy1 - y1) * 0.3f + tmpy + dddfy * 0.16666667f;
					curveLength = (float)Math.Sqrt(dfx * dfx + dfy * dfy);
					segments[0] = curveLength;
					for (ii = 1; ii < 8; ii++) {
						dfx += ddfx;
						dfy += ddfy;
						ddfx += dddfx;
						ddfy += dddfy;
						curveLength += (float)Math.Sqrt(dfx * dfx + dfy * dfy);
						segments[ii] = curveLength;
					}
					dfx += ddfx;
					dfy += ddfy;
					curveLength += (float)Math.Sqrt(dfx * dfx + dfy * dfy);
					segments[8] = curveLength;
					dfx += ddfx + dddfx;
					dfy += ddfy + dddfy;
					curveLength += (float)Math.Sqrt(dfx * dfx + dfy * dfy);
					segments[9] = curveLength;
					segment = 0;
				}

				// 按细分段长度分配权重。 原注： Weight by segment length.
				p *= curveLength;
				for (;; segment++) {
					float length = segments[segment];
					if (p > length) continue;
					if (segment == 0)
						p /= length;
					else {
						float prev = segments[segment - 1];
						p = segment + (p - prev) / (length - prev);
					}
					break;
				}
				AddCurvePosition(p * 0.1f, x1, y1, cx1, cy1, cx2, cy2, x2, y2, output, o, tangents || (i > 0 && space < PathConstraint.Epsilon));
			}
			return output;
		}

		/// <summary>沿路径起点切线向前延伸，写入位于路径起点之前的位置。</summary>
		static void AddBeforePosition (float p, float[] temp, int i, float[] output, int o) {
			float x1 = temp[i], y1 = temp[i + 1], dx = temp[i + 2] - x1, dy = temp[i + 3] - y1, r = MathUtils.Atan2(dy, dx);
			output[o] = x1 + p * MathUtils.Cos(r);
			output[o + 1] = y1 + p * MathUtils.Sin(r);
			output[o + 2] = r;
		}

		/// <summary>沿路径终点切线延伸，写入超出路径终点的位置。</summary>
		static void AddAfterPosition (float p, float[] temp, int i, float[] output, int o) {
			float x1 = temp[i + 2], y1 = temp[i + 3], dx = x1 - temp[i], dy = y1 - temp[i + 1], r = MathUtils.Atan2(dy, dx);
			output[o] = x1 + p * MathUtils.Cos(r);
			output[o + 1] = y1 + p * MathUtils.Sin(r);
			output[o + 2] = r;
		}

		/// <summary>采样三次贝塞尔曲线的位置，并按需计算切线角度。</summary>
		static void AddCurvePosition (float p, float x1, float y1, float cx1, float cy1, float cx2, float cy2, float x2, float y2,
			float[] output, int o, bool tangents) {
			if (p < PathConstraint.Epsilon || float.IsNaN(p)) {
				output[o] = x1;
				output[o + 1] = y1;
				output[o + 2] = (float)Math.Atan2(cy1 - y1, cx1 - x1);
				return;
			}
			float tt = p * p, ttt = tt * p, u = 1 - p, uu = u * u, uuu = uu * u;
			float ut = u * p, ut3 = ut * 3, uut3 = u * ut3, utt3 = ut3 * p;
			float x = x1 * uuu + cx1 * uut3 + cx2 * utt3 + x2 * ttt, y = y1 * uuu + cy1 * uut3 + cy2 * utt3 + y2 * ttt;
			output[o] = x;
			output[o + 1] = y;
			if (tangents) {
				if (p < 0.001f)
					output[o + 2] = (float)Math.Atan2(cy1 - y1, cx1 - x1);
				else
					output[o + 2] = (float)Math.Atan2(y - (y1 * uu + cy1 * ut * 2 + cy2 * tt), x - (x1 * uu + cx1 * ut * 2 + cx2 * tt));
			}
		}

		/// <summary><para>沿路径的位置，单位由 PositionMode 决定。</para>The position along the path.</summary>
		public float Position { get { return position; } set { position = value; } }
		/// <summary><para>骨骼间距，单位由 SpacingMode 决定。</para>The spacing between bones.</summary>
		public float Spacing { get { return spacing; } set { spacing = value; } }
		/// <summary><para>约束旋转与未约束旋转之间的混合比例，范围 0～1。</para>A percentage (0-1) that controls the mix between the constrained and unconstrained rotations.</summary>
		public float RotateMix { get { return rotateMix; } set { rotateMix = value; } }
		/// <summary><para>约束位移与未约束位移之间的混合比例，范围 0～1。</para>A percentage (0-1) that controls the mix between the constrained and unconstrained translations.</summary>
		public float TranslateMix { get { return translateMix; } set { translateMix = value; } }
		/// <summary><para>此路径约束修改的骨骼集合。</para>The bones that will be modified by this path constraint.</summary>
		public ExposedList<Bone> Bones { get { return bones; } }
		/// <summary><para>提供路径附件的目标插槽。</para>The slot whose path attachment will be used to constrained the bones.</summary>
		public Slot Target { get { return target; } set { target = value; } }
		/// <summary>是否参与当前骨架更新。</summary>
		public bool Active { get { return active; } }
		/// <summary><para>路径约束的初始配置数据。</para>The path constraint's setup pose data.</summary>
		public PathConstraintData Data { get { return data; } }
	}
}
