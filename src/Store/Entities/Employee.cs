using System;
using AppBoxCore;

namespace AppBoxStore;

public sealed class Employee : SqlEntity
{
    internal Employee() { }

    public Employee(Guid id)
    {
        _id = id;
    }

    private Guid _id;
    private string _name = null!;
    private bool _male;
    private DateTime _birthday;
    private string? _account;
    private byte[]? _password;

    public Guid Id => _id;

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            OnPropertyChanged(NAME_ID);
        }
    }

    public bool Male
    {
        get => _male;
        set
        {
            if (_male == value) return;
            _male = value;
            OnPropertyChanged(MALE_ID);
        }
    }

    public DateTime Birthday
    {
        get => _birthday;
        set
        {
            if (_birthday == value) return;
            _birthday = value;
            OnPropertyChanged(BIRTHDAY_ID);
        }
    }

    public string? Account
    {
        get => _account;
        set
        {
            if (_account == value) return;
            _account = value;
            OnPropertyChanged(ACCOUNT_ID);
        }
    }

    public byte[]? Password
    {
        get => _password;
        set
        {
            if (_password == value) return;
            _password = value;
            OnPropertyChanged(PASSWORD_ID);
        }
    }

    #region ====Overrides====

    internal static readonly ModelId MODELID =
        ModelId.Make(Consts.SYS_APP_ID, ModelType.Entity, 1, ModelLayer.SYS);

    internal const short ID_ID = 1 << IdUtil.MEMBERID_SEQ_OFFSET;
    internal const short NAME_ID = 2 << IdUtil.MEMBERID_SEQ_OFFSET;
    internal const short MALE_ID = 3 << IdUtil.MEMBERID_SEQ_OFFSET;
    internal const short BIRTHDAY_ID = 4 << IdUtil.MEMBERID_SEQ_OFFSET;
    internal const short ACCOUNT_ID = 5 << IdUtil.MEMBERID_SEQ_OFFSET;
    internal const short PASSWORD_ID = 6 << IdUtil.MEMBERID_SEQ_OFFSET;

    private static readonly short[] MemberIds =
        { ID_ID, NAME_ID, MALE_ID, BIRTHDAY_ID, ACCOUNT_ID, PASSWORD_ID };

    public override ModelId ModelId => MODELID;
    public override short[] AllMembers => MemberIds;

    public override void WriteMember(short id, IEntityMemberWriter ws, int flags)
    {
        switch (id)
        {
            case ID_ID:
                ws.WriteGuidMember(id, _id, flags);
                break;
            case NAME_ID:
                ws.WriteStringMember(id, _name, flags);
                break;
            case MALE_ID:
                ws.WriteBoolMember(id, _male, flags);
                break;
            case BIRTHDAY_ID:
                ws.WriteDateTimeMember(id, _birthday, flags);
                break;
            case ACCOUNT_ID:
                ws.WriteStringMember(id, _account, flags);
                break;
            case PASSWORD_ID:
                ws.WriteBinaryMember(id, _password, flags);
                break;
            default:
                throw new SerializationException(SerializationError.UnknownEntityMember,
                    nameof(Employee));
        }
    }

    public override void ReadMember(short id, IEntityMemberReader rs, int flags)
    {
        switch (id)
        {
            case ID_ID:
                _id = rs.ReadGuidMember(flags);
                break;
            case NAME_ID:
                _name = rs.ReadStringMember(flags);
                break;
            case MALE_ID:
                _male = rs.ReadBoolMember(flags);
                break;
            case BIRTHDAY_ID:
                _birthday = rs.ReadDateTimeMember(flags);
                break;
            case ACCOUNT_ID:
                _account = rs.ReadStringMember(flags);
                break;
            case PASSWORD_ID:
                _password = rs.ReadBinaryMember(flags);
                break;
            default:
                throw new SerializationException(SerializationError.UnknownEntityMember,
                    nameof(Employee));
        }
    }

    #endregion
}