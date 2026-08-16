using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pickleball
{
    /// <summary>
    /// Một dòng thông báo gắn với một lỗi luật cụ thể.
    /// <para>
    /// <see cref="localizationKey"/> là khoá sẽ dùng khi hệ thống đa ngôn ngữ được gắn vào;
    /// hiện tại <see cref="fallbackText"/> được hiển thị trực tiếp.
    /// </para>
    /// </summary>
    [Serializable]
    public class RuleMessage
    {
        /// <summary>Lỗi luật mà dòng thông báo này mô tả.</summary>
        public RuleType ruleType;

        /// <summary>Khoá localization (để dành cho Phase sau).</summary>
        public string localizationKey;

        /// <summary>Nội dung hiển thị khi chưa có localization.</summary>
        [TextArea(2, 4)]
        public string fallbackText;
    }

    /// <summary>
    /// Kho thông báo gameplay: mô tả lỗi luật, deuce, match point, advantage, game over.
    /// Đặt asset này vào <c>GameManager.gameMessages</c> và <c>ScoreManager.gameMessages</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "GameMessages", menuName = "ScriptableObjects/GameMessages")]
    public class GameMessages : ScriptableObject
    {
        /// <summary>Danh sách thông báo cho từng <see cref="RuleType"/>.</summary>
        public List<RuleMessage> ruleMessages = new List<RuleMessage>();

        /// <summary>Khoá/nội dung thông báo khi tỉ số vào thế deuce.</summary>
        public string deuceKey = "Deuce!";

        /// <summary>Khoá/nội dung thông báo khi một bên đang ở điểm match point.</summary>
        public string matchPointKey = "Match Point!";

        /// <summary>Khoá/nội dung thông báo khi một bên đang có lợi thế (advantage) trong deuce.</summary>
        public string advantageKey = "Advantage!";

        /// <summary>Khoá/nội dung thông báo khi trận đấu kết thúc.</summary>
        public string gameOverKey = "Game Over!";

        /// <summary>
        /// Lấy nội dung hiển thị cho một lỗi luật. Trả về <see cref="RuleMessage.fallbackText"/>;
        /// nếu chưa cấu hình thì trả về tên enum để dev vẫn thấy được nguyên nhân.
        /// </summary>
        /// <param name="type">Lỗi luật cần tra.</param>
        public string GetRuleMessage(RuleType type)
        {
            if (ruleMessages != null)
            {
                for (int i = 0; i < ruleMessages.Count; i++)
                {
                    RuleMessage message = ruleMessages[i];
                    if (message == null || message.ruleType != type) continue;
                    if (!string.IsNullOrEmpty(message.fallbackText)) return message.fallbackText;
                    if (!string.IsNullOrEmpty(message.localizationKey)) return message.localizationKey;
                    break;
                }
            }

            return type == RuleType.None ? string.Empty : type.ToString();
        }
    }
}
