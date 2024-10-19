using UnityEngine;
using Verse;

namespace DressPatient
{
	public class DressPatientMod: Mod
	{
		public static DressPatientsSettings settings; 
        
		public DressPatientMod(ModContentPack content) : base(content)
		{
			settings = GetSettings<DressPatientsSettings>();
		}
        
		public override void DoSettingsWindowContents(Rect inRect)
		{
			base.DoSettingsWindowContents(inRect);
			settings.DoSettingsWindowContents(inRect);
		}
        
		public override string SettingsCategory()
		{
			return Content.Name;
		}
	}
	
	
	public class DressPatientsSettings : ModSettings
	{
		public bool dressPrisoners = true;
        
		public void DoSettingsWindowContents(Rect inRect)
		{
			Rect rect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
			Listing_Standard listingStandard = new Listing_Standard();
			listingStandard.Begin(rect);
			listingStandard.CheckboxLabeled("DressPrisonersConfig".Translate(), ref dressPrisoners);
			listingStandard.End();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref dressPrisoners, "dressPrisoners", true);
		}
	}
}