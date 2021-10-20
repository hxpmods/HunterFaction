using BepInEx;
using Npc.Parts;
using BasicMod;
using BasicMod.Factories;
using System;
using UnityEngine;
using Npc.Parts.Settings;

namespace HunterFaction
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    [BepInDependency("potioncraft.basicmod")]
    public class HunterFaction : BaseUnityPlugin
    {
        public const string pluginGuid = "potioncraft.hxp.hunterfaction";
        public const string pluginName = "Hunter Faction";
        public const string pluginVersion = "0.0.1";

        private static NpcTemplate hunterQuests;

        /*  Info: 
         *      Parts:
         *              Parts, either AppearancePart or NonAppearancePart are all the things that make up a spawned in game npc
         *              This includes their dialog, their looks, their potion requests etc
         * 
         *      NpcTemplate:
         *              An NpcTemplate is a holder of parts. When an npc template is used to spawn an in game npc,
         *              all of it's baseParts are added to that NPC.
         *              
         *              In addition, NpcTemplate has a groupsOfContainers array. This holds groups of parts.
         *              When an NpcTemplate is spawned each game, one part from each group in that array is picked to be spawned with it as well.
         * 
         *              AppearanceContainer: NpcTemplates have an appearance field. This AppearanceContainer has all of the appearance parts that this NpcTemplate will try to spawn.
         *              Each field within AppearanceContainer contains a single group, picking one object from each to spawn in game.
         *              
         *              NpcTemplate's are also considered Parts. This means they can be used recursively for complex behaviour.
         *              The "Demo2GroundhogRandom_Npc" for instance, is the template for *all* customers during GroundhogDay (Pure rng that starts after the second week of the game)
         *              The tree for Groundhog day Npc generation looks somewhat like this:
         *              
         *              Demo2GroundhogRandom_Npc.groupsOfContainers[0]
         *                                  -> Faction1Container
         *                                  -> Faction2Container
         *                                  -> ...etc
         *                                      ->FactionXContainer.groupsOfContainers[0]
         *                                              ->FactionTemplateMale
         *                                              ->FactionTemplateFemale
         *                                              ->FactionTemplateWithBeard
         *                                              ->...etc
         *                                                  ->FactionTemplate.baseParts[0]
         *                                                      ->FactionPrefab (this is a game object, not an Npc template.)
         *                                                  ->FactionTemplate.baseParts[1]
         *                                                      ->FactionQuests
         *                                                          ->FactionQuests.groupsOfContainers[0]
         *                                                              -> Quest 1
         *                                                              -> Quest 2
         *                                                              -> Quest 3
         *                                                              -> SubfactionQuests (Used with Necromancy, for example)
         *                                                              -> ...etc
         *                                                  ->FactionTemplate.appearance
         *                                                      ->FactionTemplate.appearance.face
         *                                                          ->Face 1
         *                                                          ->Face 2
         *                                                          -> ...etc
         *                                                      ->FactionTemplate.appearance.etc
         *                                                          ->...etc 1
         *                                                          ->...etc 2
         *                                                          -> ...etc
         *               
         *              The tree structure also seems to allow for overwriting, you could for instance put the same appearance part at different parts of the tree                                           
         *                                                        
         *       PartContainerGroup:                                                       
         *            PartContainerGroups are used to randomly select parts. Arrays of these are stored in NpcTemplate.groupsOfContainers.
         *            They have some properties, including a group name that is mainly there for labelling purpose. They also have their own chance.
         *            Assumedly, this is the chance that an item from a group will be selected at all.
         *            
         *            Their most important property is .partsInGroup, an array of PartContainers, one of which whill get chosen from this group.
         *            
         *       PartContainer
         *            PartContainers are used by PartCOntainerGroups to randomly select parts. They have a .part property, which holds a reference to the part they are too add.
         *            And a chanceBtwParts. This appears to be a weighted value used in the selection of parts by the PartContainerGroup. Vanilla precedent seems to prefer having them all equal within the group.
         *            
         *              
         *              
         */



        public void Awake()
        {
            QuestFactory.onFactionsPreGenerate += (_, e) =>
            {
                //We create our hunterQuests and store a reference to them. This is important as MakeHunterTemplate will add them to our template.
                hunterQuests = CreateHunterQuests();
                AddFaction();
            };
        }

        public static void AddFaction()
        {

            NpcTemplate groundhogDayTemplate = NpcTemplate.GetByName("Demo2GroundhogRandom_Npc");
            //Vanilla factions all have these containers that hold different archetypes of their faction.
            //When an groundhog day npc is spawned, it first picks one of these containers, one of these containers will then pick one of these archetypes.
            NpcTemplate hunterFactionContainer = QuestFactory.CreateEmptyNpcTemplate("GroundhogDayHunterContainer");

            //This adds our template to the first groupOfContainers in the groundhogDayTemplate, and then rebalances the chances.
            //Add this point, our hunterFactionContainer is now a valid option to be picked by Demo2GroundhogRandom_Npc
            QuestFactory.AddTemplateToOtherTemplateContainerGroup(hunterFactionContainer, groundhogDayTemplate);


            //This part container group houses the different archetypes. We create a new one and set it's name in line with vanilla practise.
            var appearanceContainerGroup = new PartContainerGroup<NonAppearancePart>();
            appearanceContainerGroup.groupName = "Appearance Group";

            //We then initalise an array for our factions groupsOfContainers, setting our appearance group as it's first index.
            //In theory, one item is picked out of each containerGroup in this array.
            //You could for instance, add a group of hagglingBonus's at the second index
            hunterFactionContainer.groupsOfContainers = new PartContainerGroup<NonAppearancePart>[] { appearanceContainerGroup };

            //We create the part containers that will hold our archetype. 
            //We manually set their chances to 50/50
            var archetype1 = new PartContainer<NonAppearancePart>();
            archetype1.chanceBtwParts = 50f;

            var archetype2 = new PartContainer<NonAppearancePart>();
            archetype2.chanceBtwParts = 50f;

            //We then set the two archetypes as options in our appearanceContainer. Atleast one and only one option will be selected from this list.
            appearanceContainerGroup.partsInGroup = new PartContainer<NonAppearancePart>[] {
                archetype1, archetype2
            };

            //We feed in the male and female templates to create our hunter templates.
            NpcTemplate huntermTemplate = MakeHunterTemplate(NpcTemplate.GetByName("CitizenMTemplate 1"), false);
            NpcTemplate hunterfTemplate = MakeHunterTemplate(NpcTemplate.GetByName("CitizenFTemplate 1"), true);


            //We then set the part of our archetype PartContainers to our hunter templates. 
            archetype1.part = huntermTemplate;
            archetype2.part = hunterfTemplate;

            
            //We add our templates to the list of all npc templates.
            //This is important if you want other mods to interact with your templates, relevant for us as we use BasicMod to add our customer requests.
            NpcTemplate.allNpcTemplates.Add(hunterQuests);
            NpcTemplate.allNpcTemplates.Add(huntermTemplate);
            NpcTemplate.allNpcTemplates.Add(hunterfTemplate);
       
        }

        public static NpcTemplate CreateHunterQuests()
        {
            //We create a blank template called HunterQuests. This keeps it inline with vanilla, and allows us to add quests via BasicMods quest loader, as it searches for templates ending in "Quests"
            NpcTemplate hunterQuests = QuestFactory.CreateEmptyNpcTemplate("HunterQuests");

            //We create a new part container group named Potion Orders
            PartContainerGroup<NonAppearancePart> potionOrders = new PartContainerGroup<NonAppearancePart>();
            potionOrders.groupName = "Potion Orders";

            //We set our groupsOfContainers to a new array with our potionOrders as the first index
            hunterQuests.groupsOfContainers = new PartContainerGroup<NonAppearancePart>[] { potionOrders };

            return hunterQuests;
        }

        public static NpcTemplate MakeHunterTemplate(NpcTemplate template, bool isFemale)
        {
            //We create a copy of another template, saving us a bunch of time and leg work.
            template = Instantiate(template);

            //We get a reference to the in game hunter template.
            //There is also an in game hunter prefab, but we copy the old one instead
            var hunterTemplate = NpcTemplate.GetByName("MonsterHunterNpc 1");

            //We set our copied templates appearances to the hunter's appearance
            template.appearance.body = hunterTemplate.appearance.body; //This is his armor
            template.appearance.behindBodyFeature1 = hunterTemplate.appearance.behindBodyFeature1; //This is his sword


            //Create a new Prefab part for our faction, copying the citizen male template
            Prefab prefabPart = (Prefab)Instantiate(NpcTemplate.GetByName("CitizenMTemplate 1").baseParts[0]); //Copying the same template we pass in causes odd placement on the female face
            GameObject prefab = prefabPart.prefab;

            //Hide the hinge from the prefab
            var hinge = prefab.transform.Find("Anchor/Body/Hinge").gameObject;
            hinge.SetActive(false);

            if (isFemale)
            {
                //Copy the prefab
                prefab = Instantiate(prefab);

                //Find the pupils and offset them to stop bug eyed stare of death bug
                var leftPupil = prefab.transform.Find("Anchor/Head/Face/Pupils/Left Pupil").gameObject;
                var rightPupil = prefab.transform.Find("Anchor/Head/Face/Pupils/Right Pupil").gameObject;

                leftPupil.transform.localPosition = new Vector3(-0.08f, 0.06f, 0f);
                rightPupil.transform.localPosition = new Vector3(0.61f, 0.045f, 0f);

                //Set our prefabparts prefab to our new prefab
                prefabPart.prefab = prefab;

                template.appearance.breastSize = new PartContainerGroup<Npc.Parts.Appearance.Breast>();

            }

            //prefab.prefab = Instantiate(prefab.prefab);
           

            template.baseParts[0] = prefabPart;
            //We add our hunterQuests to the second index in our template.
            template.baseParts[1] = hunterQuests;


            return template;
        }



    }
}
