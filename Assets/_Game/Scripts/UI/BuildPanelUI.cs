using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class BuildPanelUI : MonoBehaviour
    {
        private enum BuildTab
        {
            Runes = 0,
            Skills = 1
        }

        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Tabs")]
        [SerializeField] private Button runesTabButton;
        [SerializeField] private Button skillsTabButton;
        [SerializeField] private GameObject runesPage;
        [SerializeField] private GameObject skillsPage;

        [Header("Shared")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button closeButton;

        [Header("Runes")]
        [SerializeField] private Text runesBodyText;
        [SerializeField] private Button allocateButton;
        [SerializeField] private Button refundButton;
        [SerializeField] private Button nextRuneButton;
        [SerializeField] private Button prevRuneButton;

        [Header("Skills")]
        [SerializeField] private Text skillsBodyText;
        [SerializeField] private Button equipActive1Button;
        [SerializeField] private Button equipActive2Button;
        [SerializeField] private Button equipPassiveButton;
        [SerializeField] private Button unequipButton;
        [SerializeField] private Button nextSkillButton;
        [SerializeField] private Button prevSkillButton;

        [Header("Services")]
        [SerializeField] private Game.Build.RuneService runeService;
        [SerializeField] private Game.Build.SkillService skillService;

        private BuildTab currentTab = BuildTab.Runes;
        private readonly List<Game.Data.RuneNodeData> runeNodes = new List<Game.Data.RuneNodeData>();
        private readonly List<Game.Data.SkillData> skillList = new List<Game.Data.SkillData>();
        private int runeIndex;
        private int skillIndex;
        private Game.Build.SkillSlot pendingSkillSlot = Game.Build.SkillSlot.Active1;

        public void Bind(Game.Build.RuneService runes, Game.Build.SkillService skills)
        {
            runeService = runes;
            skillService = skills;
            WireButtons();
            RebuildCaches();
            Refresh();
        }

        public void Open()
        {
            ResolveRefs();
            RebuildCaches();
            GameObject root = panelRoot != null ? panelRoot : gameObject;
            root.SetActive(true);
            gameObject.SetActive(true);
            UIPanelSlider.OpenRoot(root);
            ShowTab(BuildTab.Runes);
            Refresh();
        }

        public void Close()
        {
            UIPanelSlider.CloseRoot(panelRoot, gameObject);
        }

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            WireButtons();
        }

        private void OnEnable()
        {
            ResolveRefs();
            Subscribe();
            RebuildCaches();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveRefs()
        {
            if (runeService == null)
            {
                runeService = FindFirstObjectByType<Game.Build.RuneService>();
            }

            if (skillService == null)
            {
                skillService = FindFirstObjectByType<Game.Build.SkillService>();
            }
        }

        private void Subscribe()
        {
            if (runeService != null)
            {
                runeService.Changed -= Refresh;
                runeService.Changed += Refresh;
            }

            if (skillService != null)
            {
                skillService.Changed -= Refresh;
                skillService.Changed += Refresh;
            }
        }

        private void Unsubscribe()
        {
            if (runeService != null)
            {
                runeService.Changed -= Refresh;
            }

            if (skillService != null)
            {
                skillService.Changed -= Refresh;
            }
        }

        private void WireButtons()
        {
            Wire(closeButton, Close);
            Wire(runesTabButton, () => ShowTab(BuildTab.Runes));
            Wire(skillsTabButton, () => ShowTab(BuildTab.Skills));
            Wire(allocateButton, OnAllocate);
            Wire(refundButton, OnRefund);
            Wire(nextRuneButton, () =>
            {
                if (runeNodes.Count == 0) return;
                runeIndex = (runeIndex + 1) % runeNodes.Count;
                Refresh();
            });
            Wire(prevRuneButton, () =>
            {
                if (runeNodes.Count == 0) return;
                runeIndex = (runeIndex - 1 + runeNodes.Count) % runeNodes.Count;
                Refresh();
            });
            Wire(equipActive1Button, () =>
            {
                pendingSkillSlot = Game.Build.SkillSlot.Active1;
                OnEquipSelected();
            });
            Wire(equipActive2Button, () =>
            {
                pendingSkillSlot = Game.Build.SkillSlot.Active2;
                OnEquipSelected();
            });
            Wire(equipPassiveButton, () =>
            {
                pendingSkillSlot = Game.Build.SkillSlot.Passive;
                OnEquipSelected();
            });
            Wire(unequipButton, OnUnequipPending);
            Wire(nextSkillButton, () =>
            {
                if (skillList.Count == 0) return;
                skillIndex = (skillIndex + 1) % skillList.Count;
                Refresh();
            });
            Wire(prevSkillButton, () =>
            {
                if (skillList.Count == 0) return;
                skillIndex = (skillIndex - 1 + skillList.Count) % skillList.Count;
                Refresh();
            });
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void ShowTab(BuildTab tab)
        {
            currentTab = tab;
            if (runesPage != null)
            {
                runesPage.SetActive(tab == BuildTab.Runes);
            }

            if (skillsPage != null)
            {
                skillsPage.SetActive(tab == BuildTab.Skills);
            }

            Refresh();
        }

        private void RebuildCaches()
        {
            runeNodes.Clear();
            skillList.Clear();

            if (runeService != null && runeService.Catalog != null && runeService.Catalog.nodes != null)
            {
                for (int i = 0; i < runeService.Catalog.nodes.Count; i++)
                {
                    if (runeService.Catalog.nodes[i] != null)
                    {
                        runeNodes.Add(runeService.Catalog.nodes[i]);
                    }
                }
            }

            if (skillService != null && skillService.Catalog != null && skillService.Catalog.skills != null)
            {
                for (int i = 0; i < skillService.Catalog.skills.Count; i++)
                {
                    if (skillService.Catalog.skills[i] != null)
                    {
                        skillList.Add(skillService.Catalog.skills[i]);
                    }
                }
            }

            if (runeNodes.Count > 0)
            {
                runeIndex = Mathf.Clamp(runeIndex, 0, runeNodes.Count - 1);
            }

            if (skillList.Count > 0)
            {
                skillIndex = Mathf.Clamp(skillIndex, 0, skillList.Count - 1);
            }
        }

        public void Refresh()
        {
            ResolveRefs();
            if (titleText != null)
            {
                titleText.text = "BUILD";
            }

            if (summaryText != null && runeService != null)
            {
                summaryText.text =
                    $"Lv.{runeService.GetCharacterLevel()}  Points {runeService.GetRemaining()}/{runeService.GetTotalPoints()}  " +
                    $"Branch cap {runeService.GetBranchCap()}  " +
                    $"ATK+{runeService.GetTotalAttackBonus() + (skillService != null ? skillService.GetPassiveAttackBonus() : 0)}  " +
                    $"HP+{runeService.GetTotalHealthBonus() + (skillService != null ? skillService.GetPassiveHealthBonus() : 0)}  " +
                    $"CDR {(runeService.GetCooldownReducePercent() * 100f):0}%";
            }

            if (currentTab == BuildTab.Runes)
            {
                RefreshRunes();
            }
            else
            {
                RefreshSkills();
            }
        }

        private void RefreshRunes()
        {
            if (runesBodyText == null)
            {
                return;
            }

            if (runeService == null || runeNodes.Count == 0)
            {
                runesBodyText.text = "No runes.";
                return;
            }

            Game.Data.RuneNodeData selected = runeNodes[Mathf.Clamp(runeIndex, 0, runeNodes.Count - 1)];
            int rank = runeService.GetRank(selected.runeId);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Selected: {selected.displayName} [{selected.branch}]");
            sb.AppendLine($"Rank {rank}/{selected.maxRank}  Req Lv.{selected.requiredLevel}");
            sb.AppendLine($"Per rank: ATK+{selected.attackBonusPerRank} HP+{selected.healthBonusPerRank} CDR+{selected.cooldownReducePercentPerRank * 100f:0.#}%");
            sb.AppendLine();
            sb.AppendLine($"Defense spent {runeService.GetBranchSpent(Game.Data.RuneBranch.Defense)}/{runeService.GetBranchCap()}");
            sb.AppendLine($"Attack spent {runeService.GetBranchSpent(Game.Data.RuneBranch.Attack)}/{runeService.GetBranchCap()}");
            sb.AppendLine($"Utility spent {runeService.GetBranchSpent(Game.Data.RuneBranch.Utility)}/{runeService.GetBranchCap()}");
            sb.AppendLine();
            for (int i = 0; i < runeNodes.Count; i++)
            {
                Game.Data.RuneNodeData node = runeNodes[i];
                int r = runeService.GetRank(node.runeId);
                string mark = i == runeIndex ? ">" : " ";
                sb.AppendLine($"{mark} {node.displayName} ({node.branch}) {r}/{node.maxRank}");
            }

            runesBodyText.text = sb.ToString();
        }

        private void RefreshSkills()
        {
            if (skillsBodyText == null)
            {
                return;
            }

            if (skillService == null)
            {
                skillsBodyText.text = "No skills.";
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(FormatSlotLine(Game.Build.SkillSlot.Active1, "Active 1"));
            sb.AppendLine(FormatSlotLine(Game.Build.SkillSlot.Active2, "Active 2"));
            sb.AppendLine(FormatSlotLine(Game.Build.SkillSlot.Passive, "Passive"));
            sb.AppendLine();

            if (skillList.Count == 0)
            {
                sb.AppendLine("No skill catalog entries.");
            }
            else
            {
                Game.Data.SkillData selected = skillList[Mathf.Clamp(skillIndex, 0, skillList.Count - 1)];
                sb.AppendLine($"Selected: {selected.displayName} [{selected.kind}]");
                sb.AppendLine($"Req Lv.{selected.requiredLevel}");
                if (selected.kind == Game.Data.SkillKind.Active)
                {
                    sb.AppendLine($"CD {selected.cooldown:0.#}s  x{selected.damageMultiplier:0.##} ATK");
                }
                else
                {
                    sb.AppendLine($"Passive ATK+{selected.attackBonus} HP+{selected.healthBonus}");
                }

                sb.AppendLine();
                for (int i = 0; i < skillList.Count; i++)
                {
                    Game.Data.SkillData skill = skillList[i];
                    string mark = i == skillIndex ? ">" : " ";
                    sb.AppendLine($"{mark} {skill.displayName} ({skill.kind})");
                }
            }

            skillsBodyText.text = sb.ToString();
        }

        private string FormatSlotLine(Game.Build.SkillSlot slot, string label)
        {
            bool unlocked = skillService.IsSlotUnlocked(slot);
            Game.Data.SkillData equipped = skillService.GetEquipped(slot);
            string name = equipped != null ? equipped.displayName : "(empty)";
            if (!unlocked)
            {
                return $"{label}: LOCKED (Lv.{skillService.GetSlotUnlockLevel(slot)})";
            }

            return $"{label}: {name}";
        }

        private void OnAllocate()
        {
            if (runeService == null || runeNodes.Count == 0)
            {
                return;
            }

            Game.Data.RuneNodeData node = runeNodes[Mathf.Clamp(runeIndex, 0, runeNodes.Count - 1)];
            if (runeService.TryAllocate(node.runeId))
            {
                SetStatus($"+1 {node.displayName}");
            }
            else if (!runeService.CanAllocate(node.runeId, out string reason))
            {
                SetStatus(reason);
            }

            Refresh();
        }

        private void OnRefund()
        {
            if (runeService == null || runeNodes.Count == 0)
            {
                return;
            }

            Game.Data.RuneNodeData node = runeNodes[Mathf.Clamp(runeIndex, 0, runeNodes.Count - 1)];
            if (runeService.TryRefund(node.runeId))
            {
                SetStatus($"-1 {node.displayName}");
            }
            else
            {
                SetStatus("Nothing to refund");
            }

            Refresh();
        }

        private void OnEquipSelected()
        {
            if (skillService == null || skillList.Count == 0)
            {
                return;
            }

            Game.Data.SkillData skill = skillList[Mathf.Clamp(skillIndex, 0, skillList.Count - 1)];
            if (skillService.TryEquip(skill.skillId, pendingSkillSlot))
            {
                SetStatus($"Equipped {skill.displayName} → {pendingSkillSlot}");
            }
            else if (!skillService.CanEquip(skill.skillId, pendingSkillSlot, out string reason))
            {
                SetStatus(reason);
            }

            Refresh();
        }

        private void OnUnequipPending()
        {
            if (skillService == null)
            {
                return;
            }

            if (skillService.TryUnequip(pendingSkillSlot))
            {
                SetStatus($"Cleared {pendingSkillSlot}");
            }

            Refresh();
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message ?? string.Empty;
            }
        }
    }
}
