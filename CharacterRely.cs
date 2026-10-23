using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using CharacterRely.Content.ModCharacter;

namespace CharacterRely
{
	/*TODO : 提供角色动作播放器 选择使用Spine 作为播放器 然后提供按钮类 用来存储选取角色对象
	 提供一个数组在Load模组的时候用来给以及做好的角色增加按钮 以便于选取或者后期制作 角色图标默认为40*40（目前如此认为）
	 制作一个角色选取确认界面 上面显示角色被动效果 以及角色技能 以及角色立绘
	 模组角色抽象类要求 提供图层更改功能 适配原版手臂以及 提供动作接口以及制作几个常规的动作 提供技能接口*/
	public class CharacterRely : Mod
	{
		public override void Load()
		{
			// 公共系统由同一份 DLL 承载，所有角色共用注册表、界面和绘制入口。
			CharacterRelyLibrary.Initialize(this);
			CharacterRegistry.Register<TestCharacter>(this, "test", "测试角色",
				"用于演示 Spine 动画、手持物品位置和技能系统的示例角色。");
		}

		public override void HandlePacket(System.IO.BinaryReader reader, int whoAmI)
		{
			if (reader.ReadByte() == CharacterRelyLibrary.PacketId)
				CharacterRelyLibrary.HandlePacket(reader, whoAmI);
		}
	}
}
