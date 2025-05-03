namespace VRCXDiscordTracker.Core.VRCX;

/// <summary>
/// ����������/����C���X�^���X�̏����i�[����N���X
/// </summary>
internal class MyLocation
{
    /// <summary>
    /// �Q��ID
    /// </summary>
    public required long JoinId { get; set; }

    /// <summary>
    /// ���[�U�[ID
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// ���[�U�[��
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// ���P�[�V����ID
    /// </summary>
    public required string Location { get; set; }

    /// <summary>
    /// �C���X�^���X�ɎQ����������
    /// </summary>
    public required DateTime JoinCreatedAt { get; set; }

    /// <summary>
    /// Join�C�x���g��Time�l�B��{�I��0�ɂȂ�B
    /// </summary>
    public required long JoinTime { get; set; }

    /// <summary>
    /// �ޏoID
    /// </summary>
    public required long? LeaveId { get; set; }

    /// <summary>
    /// �ޏo����
    /// </summary>
    public required DateTime? LeaveCreatedAt { get; set; }

    /// <summary>
    /// �ޏo�C�x���g��Time�l�B�C���X�^���X�ɋ������� (�~���b)
    /// </summary>
    public required long? LeaveTime { get; set; }

    /// <summary>
    /// ���ɈقȂ�C���X�^���X�ɎQ����������
    /// </summary>
    public required DateTime? NextJoinCreatedAt { get; set; }

    /// <summary>
    /// �����炭���̃C���X�^���X��ޏo��������
    /// </summary>
    public required DateTime? EstimatedLeaveCreatedAt { get; set; }

    /// <summary>
    /// ���[���h�̖��O
    /// </summary>
    public required string? WorldName { get; set; }

    /// <summary>
    /// ���[���h��ID
    /// </summary>
    public required string? WorldId { get; set; }
}
