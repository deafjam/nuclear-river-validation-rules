using NuClear.ValidationRules.Storage.Model.Messages;

namespace NuClear.ValidationRules.Querying.Host.Composition
{
    // TODO: отрефакторить IMessageSeverityProvider и ICheckModeDescriptor, постараться свести всё к одному
    public sealed class MessageSeverityProvider : IMessageSeverityProvider
    {
        public RuleSeverityLevel GetSeverityLevel(CheckMode checkMode, Message message)
        {
            switch (message.MessageType)
            {
                case MessageTypeCode.OrderRequiredFieldsShouldBeSpecified:
                    // Понижаем уровень ошибки LegalPersonProfile до Warning для не Single проверок
                    // т.к. обычный заказ не может быть утвержден без выполнения этой проверки (error в Single проверке),
                    // а для самопродажных заказов она не имеет смысла (поэтому warning в массовых проверках)
                    var isLegalPersonProfile = bool.Parse(message.Extra["legalPersonProfile"]);
                    var isCurrency = bool.Parse(message.Extra["currency"]);
                    var isBranchOfficeOrganizationUnit = bool.Parse(message.Extra["branchOfficeOrganizationUnit"]);
                    var isLegalPerson = bool.Parse(message.Extra["legalPerson"]);

                    if (checkMode != CheckMode.Single && !isCurrency
                                                      && !isBranchOfficeOrganizationUnit && !isLegalPerson && isLegalPersonProfile)
                    {
                        return RuleSeverityLevel.Warning;
                    }
                    break;
                
                case MessageTypeCode.LinkedFirmAddressShouldBeValid:
                    var isPartnerAddress = bool.Parse(message.Extra["isPartnerAddress"]);
                    if (isPartnerAddress)
                    {
                        return RuleSeverityLevel.Warning;
                    }
                    break;
                
                case MessageTypeCode.PoiAmountForEntranceShouldMeetMaximumRestrictions:
                    var isSameAddress = bool.Parse(message.Extra["isSameAddress"]);
                    if (isSameAddress)
                    {
                        return RuleSeverityLevel.Error;
                    }
                    break;
            }

            return CheckModeRegistry.GetSeverityLevel(checkMode, message.MessageType);
        }
    }
}