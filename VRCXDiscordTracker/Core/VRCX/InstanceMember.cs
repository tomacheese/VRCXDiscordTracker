namespace VRCXDiscordTracker.Core.VRCX;

/// <summary>
/// VRCX�̃C���X�^���X�����o�[��\���N���X
/// </summary>
internal class InstanceMember
{
    /// <summary>
    /// �C���X�^���X�����o�[��ID
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// �C���X�^���X�����o�[�̖��O
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// �ŏI�Q������
    /// </summary>
    public required DateTime LastJoinAt { get; set; }

    /// <summary>
    /// �ŏI�ޏo����
    /// </summary>
    public required DateTime? LastLeaveAt { get; set; }

    /// <summary>
    /// ���݂��C���X�^���X�ɎQ�����Ă��邩�ǂ���
    /// </summary>
    public required bool IsCurrently { get; set; }

    /// <summary>
    /// �C���X�^���X�̃I�[�i�[���ǂ���
    /// </summary>
    public required bool IsInstanceOwner { get; set; }

    /// <summary>
    /// �t�����h���ǂ���
    /// </summary>
    public required bool IsFriend { get; set; }
}
