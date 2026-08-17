using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Pickleball.EditorTools
{
    /// <summary>
    /// Xuất tài liệu Markdown mô tả ĐẦY ĐỦ hai animator controller của bản gốc
    /// (<c>PlayerAnimationControllerv3</c>, <c>FemaleAnimationControllerv3</c>): parameter, layer,
    /// state, clip gắn vào và toàn bộ transition kèm điều kiện.
    ///
    /// <para><b>Mục đích</b>: đối chiếu các hằng <c>StringConstants.Anim*</c> mà
    /// <see cref="BasePlayerAnimationController"/> đang dùng với tên parameter THẬT của bản gốc.
    /// Nếu hai bên lệch nhau thì animator sẽ im lặng bỏ qua mọi lệnh <c>SetTrigger</c>/<c>SetFloat</c>
    /// và nhân vật đứng yên — lỗi rất khó nhìn ra nếu không có bảng đối chiếu này.</para>
    ///
    /// <para>Báo cáo được ghi ra <c>ui_layout/animator_report.md</c> (ngoài thư mục project).</para>
    /// </summary>
    public static class AnimatorParameterReport
    {
        // ------------------------------------------------------------------ Đường dẫn

        private const string ControllerFolder = "Assets/Project/ArtFromOriginal/Animations/Controllers";

        private static readonly string[] ControllerNames =
        {
            "PlayerAnimationControllerv3",
            "FemaleAnimationControllerv3",
            "Audience",
            "BagAnimatorGeneric"
        };

        /// <summary>Đường dẫn tuyệt đối dự phòng khi không suy được thư mục <c>ui_layout</c>.</summary>
        private const string FallbackReportPath = @"E:\pikeball\reverse-engineering\ui_layout\animator_report.md";

        // ------------------------------------------------------------------ Bảng đối chiếu

        /// <summary>
        /// Hằng số trong <see cref="StringConstants"/> mà <see cref="BasePlayerAnimationController"/>
        /// đang gửi xuống Animator, kèm kiểu mong đợi.
        /// </summary>
        private static readonly (string ConstantName, string Value, AnimatorControllerParameterType Type)[]
            ExpectedParameters =
            {
                ("AnimXSpeed", StringConstants.AnimXSpeed, AnimatorControllerParameterType.Float),
                ("AnimZSpeed", StringConstants.AnimZSpeed, AnimatorControllerParameterType.Float),
                ("AnimShotTrigger", StringConstants.AnimShotTrigger, AnimatorControllerParameterType.Trigger),
                ("AnimPreShotTrigger", StringConstants.AnimPreShotTrigger, AnimatorControllerParameterType.Trigger),
                ("AnimMissTrigger", StringConstants.AnimMissTrigger, AnimatorControllerParameterType.Trigger),
                ("AnimResetTrigger", StringConstants.AnimResetTrigger, AnimatorControllerParameterType.Trigger),
                ("AnimWinTrigger", StringConstants.AnimWinTrigger, AnimatorControllerParameterType.Trigger),
                ("AnimLoseTrigger", StringConstants.AnimLoseTrigger, AnimatorControllerParameterType.Trigger),
                ("AnimIsRightSide", StringConstants.AnimIsRightSide, AnimatorControllerParameterType.Bool),
                ("AnimIsGameplayActive", StringConstants.AnimIsGameplayActive, AnimatorControllerParameterType.Bool),
                ("AnimMirrorHandSide", StringConstants.AnimMirrorHandSide, AnimatorControllerParameterType.Bool),
                ("AnimShotTypeIndex", StringConstants.AnimShotTypeIndex, AnimatorControllerParameterType.Int)
            };

        // ------------------------------------------------------------------ Entry point

        /// <summary>Đọc các animator controller gốc và ghi báo cáo Markdown.</summary>
        [MenuItem("Pickleball/Art/Animator Parameter Report")]
        public static void GenerateReport()
        {
            var controllers = new List<AnimatorController>();

            foreach (string name in ControllerNames)
            {
                var controller =
                    AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerFolder + "/" + name + ".controller");

                if (controller == null)
                {
                    Debug.LogWarning("[AnimatorParameterReport] Không nạp được " + name + ".controller trong " +
                                     ControllerFolder);
                    continue;
                }

                controllers.Add(controller);
            }

            if (controllers.Count == 0)
            {
                Debug.LogError("[AnimatorParameterReport] Không có controller nào — chạy " +
                               "'Pickleball/Art/Import Original Character Art' trước.");
                return;
            }

            var text = new StringBuilder();
            WriteHeader(text);

            foreach (AnimatorController controller in controllers)
            {
                WriteController(text, controller);
            }

            WriteComparison(text, controllers);

            string reportPath = ResolveReportPath();
            try
            {
                string directory = Path.GetDirectoryName(reportPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                File.WriteAllText(reportPath, text.ToString(), new UTF8Encoding(false));
                Debug.Log("[AnimatorParameterReport] Đã ghi báo cáo: " + reportPath);
            }
            catch (IOException exception)
            {
                Debug.LogError("[AnimatorParameterReport] Không ghi được báo cáo: " + exception.Message);
            }
        }

        // ------------------------------------------------------------------ Phần đầu

        private static void WriteHeader(StringBuilder text)
        {
            text.AppendLine("# Báo cáo Animator của bản gốc");
            text.AppendLine();
            text.AppendLine("Sinh tự động bởi `Pickleball/Art/Animator Parameter Report`.");
            text.AppendLine();
            text.AppendLine("Nguồn: `" + ControllerFolder + "`");
            text.AppendLine();
            text.AppendLine("> Dùng bảng này để chỉnh `StringConstants.Anim*` cho khớp tên parameter THẬT.");
            text.AppendLine("> Sai tên thì Animator im lặng bỏ qua lệnh và nhân vật đứng yên.");
            text.AppendLine();
        }

        // ------------------------------------------------------------------ Một controller

        private static void WriteController(StringBuilder text, AnimatorController controller)
        {
            text.AppendLine("---");
            text.AppendLine();
            text.AppendLine("## " + controller.name);
            text.AppendLine();

            // --- Parameter ---
            text.AppendLine("### Parameter");
            text.AppendLine();
            text.AppendLine("| Tên | Kiểu | Mặc định |");
            text.AppendLine("|---|---|---|");

            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                text.AppendLine("| `" + parameter.name + "` | " + parameter.type + " | " +
                                DescribeDefault(parameter) + " |");
            }

            text.AppendLine();

            // --- Layer ---
            foreach (AnimatorControllerLayer layer in controller.layers)
            {
                text.AppendLine("### Layer: " + layer.name +
                                "  (weight " + layer.defaultWeight.ToString("0.##") +
                                ", blending " + layer.blendingMode + ")");
                text.AppendLine();

                if (layer.stateMachine == null)
                {
                    text.AppendLine("_(layer không có state machine)_");
                    text.AppendLine();
                    continue;
                }

                var states = new List<(string Path, AnimatorState State)>();
                CollectStates(layer.stateMachine, string.Empty, states);

                text.AppendLine("#### State và clip");
                text.AppendLine();
                text.AppendLine("| State | Motion (clip / blend tree) | Speed | Mirror |");
                text.AppendLine("|---|---|---|---|");

                foreach ((string path, AnimatorState state) in states)
                {
                    text.AppendLine("| `" + path + "` | " + DescribeMotion(state.motion) + " | " +
                                    state.speed.ToString("0.##") + " | " + (state.mirror ? "có" : "-") + " |");
                }

                text.AppendLine();

                text.AppendLine("#### Transition");
                text.AppendLine();
                text.AppendLine("| Từ | Tới | Điều kiện | Exit time |");
                text.AppendLine("|---|---|---|---|");

                WriteStateMachineTransitions(text, layer.stateMachine, string.Empty);

                foreach ((string path, AnimatorState state) in states)
                {
                    foreach (AnimatorStateTransition transition in state.transitions)
                    {
                        WriteTransitionRow(text, path, transition);
                    }
                }

                text.AppendLine();
            }
        }

        /// <summary>Ghi transition của Any State / Entry cho một state machine và mọi sub-state machine.</summary>
        private static void WriteStateMachineTransitions(StringBuilder text, AnimatorStateMachine stateMachine,
                                                         string prefix)
        {
            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
            {
                WriteTransitionRow(text, prefix + "Any State", transition);
            }

            foreach (AnimatorTransition transition in stateMachine.entryTransitions)
            {
                text.AppendLine("| `" + prefix + "Entry` | `" + DescribeDestination(transition.destinationState,
                                                                                    transition.destinationStateMachine) +
                                "` | " + DescribeConditions(transition.conditions) + " | - |");
            }

            foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines)
            {
                WriteStateMachineTransitions(text, child.stateMachine, prefix + child.stateMachine.name + "/");
            }
        }

        private static void WriteTransitionRow(StringBuilder text, string from, AnimatorStateTransition transition)
        {
            string destination = DescribeDestination(transition.destinationState, transition.destinationStateMachine);
            if (transition.isExit) destination = "Exit";

            string exitTime = transition.hasExitTime ? transition.exitTime.ToString("0.###") : "-";

            text.AppendLine("| `" + from + "` | `" + destination + "` | " +
                            DescribeConditions(transition.conditions) + " | " + exitTime + " |");
        }

        // ------------------------------------------------------------------ Đối chiếu StringConstants

        /// <summary>
        /// So khớp từng hằng <c>StringConstants.Anim*</c> với parameter thật của controller và
        /// chỉ rõ hằng nào phải sửa.
        /// </summary>
        private static void WriteComparison(StringBuilder text, List<AnimatorController> controllers)
        {
            text.AppendLine("---");
            text.AppendLine();
            text.AppendLine("## Đối chiếu với `StringConstants.Anim*`");
            text.AppendLine();
            text.AppendLine("| Hằng số | Giá trị hiện tại | Kiểu cần | Có trong controller nhân vật? |");
            text.AppendLine("|---|---|---|---|");

            var available = new Dictionary<string, AnimatorControllerParameterType>(StringComparer.Ordinal);
            foreach (AnimatorController controller in controllers)
            {
                if (!controller.name.Contains("AnimationControllerv3")) continue;

                foreach (AnimatorControllerParameter parameter in controller.parameters)
                {
                    available[parameter.name] = parameter.type;
                }
            }

            int mismatches = 0;

            foreach ((string constantName, string value, AnimatorControllerParameterType type) in ExpectedParameters)
            {
                string verdict;
                if (!available.TryGetValue(value, out AnimatorControllerParameterType actual))
                {
                    verdict = "**KHÔNG CÓ** — animator sẽ bỏ qua";
                    mismatches++;
                }
                else if (actual != type)
                {
                    verdict = "có nhưng SAI KIỂU (thật: " + actual + ")";
                    mismatches++;
                }
                else
                {
                    verdict = "khớp";
                }

                text.AppendLine("| `" + constantName + "` | `" + value + "` | " + type + " | " + verdict + " |");
            }

            text.AppendLine();
            text.AppendLine("Số hằng số lệch: **" + mismatches + "/" + ExpectedParameters.Length + "**");
            text.AppendLine();

            text.AppendLine("### Parameter THẬT của controller nhân vật");
            text.AppendLine();
            foreach (KeyValuePair<string, AnimatorControllerParameterType> pair in available)
            {
                text.AppendLine("- `" + pair.Key + "` — " + pair.Value);
            }

            text.AppendLine();

            Debug.Log("[AnimatorParameterReport] " + mismatches + "/" + ExpectedParameters.Length +
                      " hằng số StringConstants.Anim* KHÔNG khớp parameter thật của bản gốc.");
        }

        // ------------------------------------------------------------------ Mô tả

        /// <summary>Duyệt state machine (kể cả sub-state machine) và thu tên đầy đủ của mọi state.</summary>
        private static void CollectStates(AnimatorStateMachine stateMachine, string prefix,
                                          List<(string, AnimatorState)> result)
        {
            foreach (ChildAnimatorState child in stateMachine.states)
            {
                result.Add((prefix + child.state.name, child.state));
            }

            foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines)
            {
                CollectStates(child.stateMachine, prefix + child.stateMachine.name + "/", result);
            }
        }

        private static string DescribeDefault(AnimatorControllerParameter parameter)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Float: return parameter.defaultFloat.ToString("0.##");
                case AnimatorControllerParameterType.Int: return parameter.defaultInt.ToString();
                case AnimatorControllerParameterType.Bool: return parameter.defaultBool ? "true" : "false";
                default: return "-";
            }
        }

        /// <summary>Mô tả motion của state: tên clip, hoặc cấu trúc blend tree kèm parameter điều khiển.</summary>
        private static string DescribeMotion(Motion motion)
        {
            if (motion == null) return "_(trống)_";

            if (motion is BlendTree tree)
            {
                var description = new StringBuilder();
                description.Append("BlendTree **").Append(tree.name).Append("** (")
                           .Append(tree.blendType).Append(", `").Append(tree.blendParameter).Append('`');

                if (tree.blendType != BlendTreeType.Simple1D)
                {
                    description.Append(" / `").Append(tree.blendParameterY).Append('`');
                }

                description.Append(") → ");

                for (int i = 0; i < tree.children.Length; i++)
                {
                    if (i > 0) description.Append(", ");

                    ChildMotion child = tree.children[i];
                    description.Append(child.motion != null ? child.motion.name : "(trống)");
                }

                return description.ToString();
            }

            return "`" + motion.name + "`";
        }

        private static string DescribeDestination(AnimatorState state, AnimatorStateMachine stateMachine)
        {
            if (state != null) return state.name;
            if (stateMachine != null) return stateMachine.name + "/ (state machine)";
            return "Exit";
        }

        /// <summary>Ghép các điều kiện của một transition thành chuỗi đọc được.</summary>
        private static string DescribeConditions(AnimatorCondition[] conditions)
        {
            if (conditions == null || conditions.Length == 0) return "_(không điều kiện)_";

            var parts = new List<string>(conditions.Length);
            foreach (AnimatorCondition condition in conditions)
            {
                parts.Add("`" + condition.parameter + "` " + DescribeMode(condition.mode, condition.threshold));
            }

            return string.Join(" && ", parts);
        }

        private static string DescribeMode(AnimatorConditionMode mode, float threshold)
        {
            switch (mode)
            {
                case AnimatorConditionMode.If: return "= true (trigger)";
                case AnimatorConditionMode.IfNot: return "= false";
                case AnimatorConditionMode.Greater: return "> " + threshold.ToString("0.##");
                case AnimatorConditionMode.Less: return "< " + threshold.ToString("0.##");
                case AnimatorConditionMode.Equals: return "== " + threshold.ToString("0.##");
                case AnimatorConditionMode.NotEqual: return "!= " + threshold.ToString("0.##");
                default: return mode.ToString();
            }
        }

        // ------------------------------------------------------------------ Tiện ích

        /// <summary>Suy ra <c>&lt;repo&gt;/ui_layout/animator_report.md</c>; không thấy thì dùng đường dẫn tuyệt đối.</summary>
        private static string ResolveReportPath()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            string repoRoot = projectRoot != null ? Directory.GetParent(projectRoot)?.FullName : null;

            if (repoRoot != null)
            {
                string candidate = Path.Combine(repoRoot, "ui_layout");
                if (Directory.Exists(candidate)) return Path.Combine(candidate, "animator_report.md");
            }

            return FallbackReportPath;
        }
    }
}
