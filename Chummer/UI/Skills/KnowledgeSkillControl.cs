using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Chummer.helpers;
using Chummer.Skills;

namespace Chummer.UI.Skills
{
    public partial class KnowledgeSkillControl : UserControl
    {
        private readonly KnowledgeSkill _skill;
        public KnowledgeSkillControl(KnowledgeSkill skill)
        {
            _skill = skill;
            InitializeComponent();

            //Display
            lblModifiedRating.DataBindings.Add("Text", skill, nameof(KnowledgeSkill.DisplayPool), false, DataSourceUpdateMode.OnPropertyChanged);

            cboType.BeginUpdate();
            cboSkill.BeginUpdate();
            cboSpec.BeginUpdate();
            cboType.DisplayMember = nameof(ListItem.Name);
            cboType.ValueMember = nameof(ListItem.Value);
            cboType.DataSource = KnowledgeSkill.KnowledgeTypes;
            cboType.DataBindings.Add("SelectedValue", skill, nameof(KnowledgeSkill.Type), false, DataSourceUpdateMode.OnPropertyChanged);

            if (skill.CharacterObject.Created)
            {
                nudKarma.Visible = false;
                nudSkill.Visible = false;

                lblRating.Visible = true;
                lblRating.DataBindings.Add("Text", skill, nameof(Skill.Rating), false, DataSourceUpdateMode.OnPropertyChanged);
                
                //New knowledge skills start at 0. Leave the Type selector unlocked until they spend Karma on the skill.
                cboType.Enabled = (skill.Karma == 0 && skill.Base == 0 || string.IsNullOrWhiteSpace(_skill.Type));

                lblName.Visible = true;
                lblName.DataBindings.Add("Text", skill, nameof(KnowledgeSkill.WriteableName), false, DataSourceUpdateMode.OnPropertyChanged);

                lblSpec.Visible = true;
                lblSpec.Text = string.Join(", ", skill.Specializations.Select(x => x.Name));
                cboSkill.Visible = false;
                chkKarma.Visible = false;
                cboSpec.Visible = false;

                lblModifiedRating.Location = new Point(294 - 30, 4);

                btnAddSpec.Visible = true;
                btnAddSpec.DataBindings.Add("Enabled", skill, nameof(Skill.CanAffordSpecialization), false, DataSourceUpdateMode.OnPropertyChanged);
                btnCareerIncrease.Visible = true;
                btnCareerIncrease.DataBindings.Add("Enabled", skill, nameof(Skill.CanUpgradeCareer), false,
                    DataSourceUpdateMode.OnPropertyChanged);
                lblSpec.DataBindings.Add("Text", skill, nameof(Skill.DisplaySpecialization), false, DataSourceUpdateMode.OnPropertyChanged);
            }
            else
            {
                //Up down boxes
                nudKarma.DataBindings.Add("Value", skill, nameof(Skill.Karma), false, DataSourceUpdateMode.OnPropertyChanged);
                nudKarma.DataBindings.Add("InterceptMouseWheel", skill.CharacterObject.Options, nameof(CharacterOptions.InterceptMode), false, DataSourceUpdateMode.OnPropertyChanged);

                nudSkill.DataBindings.Add("Value", skill, nameof(Skill.Base), false, DataSourceUpdateMode.OnPropertyChanged);
                nudSkill.DataBindings.Add("Enabled", skill.CharacterObject.SkillsSection, nameof(SkillsSection.HasKnowledgePoints), false, DataSourceUpdateMode.OnPropertyChanged);
                nudSkill.DataBindings.Add("InterceptMouseWheel", skill.CharacterObject.Options, nameof(CharacterOptions.InterceptMode), false, DataSourceUpdateMode.OnPropertyChanged);

                chkKarma.DataBindings.Add("Checked", skill, nameof(Skill.BuyWithKarma), false,
                        DataSourceUpdateMode.OnPropertyChanged);
                cboSkill.DisplayMember = nameof(ListItem.Name);
                cboSkill.ValueMember = nameof(ListItem.Value);
                cboSkill.DataSource = skill.KnowledgeSkillCatagories;
                cboSkill.SelectedIndex = -1;
                cboSkill.DataBindings.Add("Text", skill, nameof(KnowledgeSkill.WriteableName), false, DataSourceUpdateMode.OnPropertyChanged);

                //dropdown/spec
                cboSpec.DisplayMember = nameof(ListItem.Name);
                cboSpec.ValueMember = nameof(ListItem.Value);
                cboSpec.DataSource = skill.CGLSpecializations;
                cboSpec.SelectedIndex = -1;

                cboSpec.DataBindings.Add("Enabled", skill, nameof(Skill.Leveled), false, DataSourceUpdateMode.OnPropertyChanged);
                cboSpec.DataBindings.Add("Text", skill, nameof(Skill.Specialization), false, DataSourceUpdateMode.OnPropertyChanged);

                skill.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(Skill.CGLSpecializations))
                    {
                        cboSpec.DataSource = null;
                        cboSpec.DisplayMember = nameof(ListItem.Name);
                        cboSpec.ValueMember = nameof(ListItem.Value);
                        cboSpec.DataSource = skill.CGLSpecializations;
                        cboSpec.MaxDropDownItems = Math.Max(1, skill.CGLSpecializations.Count);
                    }
                };
            }

            if (skill.ForcedName)
            {
                nudKarma.Visible = false;
                nudSkill.Visible = false;
                cboSkill.Enabled = false;
                cboSpec.DataBindings.Clear();
                cboSpec.Enabled = false;
                btnAddSpec.Enabled = false;
                btnCareerIncrease.Enabled = false;

                lblRating.Visible = true;
                lblRating.Text = skill.CyberwareRating().ToString();

                cmdDelete.Visible = false;
            }
            else
            {
                cmdDelete.Click += (sender, args) => { skill.CharacterObject.SkillsSection.KnowledgeSkills.Remove(skill); };
            }
            cboType.EndUpdate();
            cboSkill.EndUpdate();
            cboSpec.EndUpdate();
        }

        private void btnCareerIncrease_Click(object sender, EventArgs e)
        {
            frmCareer parent = ParentForm as frmCareer;
            if (parent != null)
            {
                int upgradeKarmaCost = _skill.UpgradeKarmaCost();

                if (upgradeKarmaCost == -1) return; //TODO: more descriptive
                string confirmstring = string.Format(LanguageManager.GetString("Message_ConfirmKarmaExpense"),
                       _skill.DisplayName, _skill.Rating + 1, upgradeKarmaCost, cboType.GetItemText(cboType.SelectedItem));

                if (!parent.ConfirmKarmaExpense(confirmstring))
                    return;
            }
            cboType.Enabled = false;

            _skill.Upgrade();
        }

        private void btnAddSpec_Click(object sender, EventArgs e)
        {
            frmCareer parrent = ParentForm as frmCareer;
            if (parrent != null)
            {
                int price = _skill.CharacterObject.Options.KarmaKnowledgeSpecialization;

                int intExtraSpecCost = 0;
                int intTotalBaseRating = _skill.TotalBaseRating;
                decimal decSpecCostMultiplier = 1.0m;
                foreach (Improvement objLoopImprovement in _skill.CharacterObject.Improvements)
                {
                    if (objLoopImprovement.Minimum <= intTotalBaseRating &&
                        (string.IsNullOrEmpty(objLoopImprovement.Condition) || (objLoopImprovement.Condition == "career") == _skill.CharacterObject.Created || (objLoopImprovement.Condition == "create") != _skill.CharacterObject.Created) && objLoopImprovement.Enabled)
                    {
                        if (objLoopImprovement.ImprovedName == _skill.SkillCategory)
                        {
                            if (objLoopImprovement.ImproveType == Improvement.ImprovementType.SkillCategorySpecializationKarmaCost)
                                intExtraSpecCost += objLoopImprovement.Value;
                            else if (objLoopImprovement.ImproveType == Improvement.ImprovementType.SkillCategorySpecializationKarmaCostMultiplier)
                                decSpecCostMultiplier *= objLoopImprovement.Value / 100.0m;
                        }
                    }
                }
                if (decSpecCostMultiplier != 1.0m)
                    price = decimal.ToInt32(decimal.Ceiling(price * decSpecCostMultiplier));
                price += intExtraSpecCost; //Spec

                string confirmstring = string.Format(LanguageManager.GetString("Message_ConfirmKarmaExpenseSkillSpecialization"), price.ToString());

                if (!parrent.ConfirmKarmaExpense(confirmstring))
                    return;
            }

            frmSelectSpec selectForm = new frmSelectSpec(_skill);
            selectForm.Mode = "Knowledge";
            selectForm.ShowDialog();

            if (selectForm.DialogResult != DialogResult.OK) return;

            _skill.AddSpecialization(selectForm.SelectedItem);

            //TODO turn this into a databinding, but i don't care enough right now
            lblSpec.Text = string.Join(", ", _skill.Specializations.Select(x => x.Name));

            parrent?.ScheduleCharacterUpdate();
        }

        private void cboSpec_TextChanged(object sender, EventArgs e)
        {
            if (!_skill.CharacterObject.Options.AllowPointBuySpecializationsOnKarmaSkills &&
                nudSkill.Value == 0 && !string.IsNullOrWhiteSpace(cboSpec.Text))
            {
                chkKarma.Checked = true;
            }
        }

        private void cboSkill_TextChanged(object sender, EventArgs e)
        {
            _skill.LoadDefaultType(_skill.Name);
        }
    }
}
