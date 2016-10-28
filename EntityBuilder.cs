void Main()
{
	using (var scope = GetContainer().BeginLifetimeScope())
	{
		var builder = scope.Resolve<IEntityBuilder<IndividualPartner>>();

		builder.Build().Dump();
	}
}

IContainer GetContainer()
{
	var builder = new ContainerBuilder();

	builder.RegisterGeneric(typeof(EntityBuilder<>)).AsImplementedInterfaces();
	builder.RegisterType<Partners>().AsImplementedInterfaces();
	builder.RegisterType<PartnersWithAddresses>().AsImplementedInterfaces();
	builder.RegisterType<PartnersWithPhones>().AsImplementedInterfaces();

	return builder.Build();
}

interface IEntityComponent<TResult>
{
	IEnumerable<TResult> Execute(IEnumerable<TResult> seed);
}

class Partners : IEntityComponent<IndividualPartner>
{
	public IEnumerable<IndividualPartner> Execute(IEnumerable<IndividualPartner> seed)
	{
		return GetPartners().Select(p => new IndividualPartner(p, ImmutableArray.Create<AddressInformation>(), ImmutableArray.Create<PhoneNumber>(), 0));
	}

	private IEnumerable<IndividualPartnerInformation> GetPartners()
	{
		return new List<IndividualPartnerInformation>
	{
		new IndividualPartnerInformation(1, "Don", "", "", "W", "", "", "en-US", false, false, true),
		new IndividualPartnerInformation(2, "Kyla", "", "", "W", "", "", "en-US", false, false, true),
		new IndividualPartnerInformation(3, "Vern", "", "", "W", "", "", "en-US", false, false, true)
	}.ToImmutableArray();
	}
}

class PartnersWithAddresses : IEntityComponent<IndividualPartner>
{
	public IEnumerable<IndividualPartner> Execute(IEnumerable<IndividualPartner> seed)
	{
		return seed.Joiner<IndividualPartner, AddressInformation, IndividualPartner>
		((a, b) => new IndividualPartner(a, b.ToImmutableArray(), ImmutableArray<PhoneNumber>.Empty, 0))
		(GetAddresses());
	}

	
	public IEnumerable<AddressInformation> GetAddresses()
	{
		return new List<AddressInformation>
	{
		new AddressInformation(1, 1, "9876", "", "City", "12345", "IA", 1, "Scott", "US", 0.0, 0.0),
		new AddressInformation(2, 1, "1234", "", "Town", "54321", "IA", 1, "Polk", "US", 0.0, 0.0),
		new AddressInformation(3, 1, "444", "", "Metropolis", "23456", "IA", 1, "Muscatine", "US", 0.0, 0.0)
	}.ToImmutableArray();
	}
}

class PartnersWithPhones : IEntityComponent<IndividualPartner>
{
	public IEnumerable<IndividualPartner> Execute(IEnumerable<IndividualPartner> seed)
	{
		return seed.Joiner<IndividualPartner, PhoneNumber, IndividualPartner>
		((a, b) => new IndividualPartner(a, a.Addresses, b.ToImmutableArray(), 0))
		(GetPhones());
	}

	public IEnumerable<PhoneNumber> GetPhones()
	{
		return new List<PhoneNumber>
	{
		new PhoneNumber(1, 1, "1234567890", 1),
		new PhoneNumber(1, 2, "5551231234", 1),
		new PhoneNumber(2, 3, "5553214321", 1)
	}.ToImmutableArray();
	}
}

interface IEntityBuilder<TType>
{
	IEnumerable<TType> Build();
}

class EntityBuilder<TType> : IEntityBuilder<TType>
{
	private readonly IEnumerable<IEntityComponent<TType>> _components;

	public EntityBuilder(IEnumerable<IEntityComponent<TType>> components)
	{
		_components = components;
	}

	public IEnumerable<TType> Build()
	{
		return this._components
			.Aggregate(new List<TType>(), (seed, curr) => curr.Execute(seed).ToList());
	}
}

static class Extensions
{
	public static Func<IEnumerable<TRight>, IEnumerable<TResult>>
		Joiner<TLeft, TRight, TResult>(this IEnumerable<TLeft> left, Func<TLeft, IEnumerable<TRight>, TResult> selector)
		where TLeft : IHasCustomerId
		where TRight : IHasCustomerId
	{
		return b => left.GroupJoin(b, l => l.CustomerId, r => r.CustomerId, selector);
	}
}

public interface IHasCustomerId
{
	int CustomerId { get; }
}

public sealed class IndividualPartnerInformation : IHasCustomerId
{
	private readonly int customerId;

	public readonly string FirstName;

	public readonly string PreferredName;

	public readonly string MiddleName;

	public readonly string LastName;

	public readonly string Suffix;

	public readonly string Email;

	public readonly string Culture;

	public readonly bool MonsantoUnauthorizedGrower;

	public readonly bool PioneerUnauthorizedGrower;

	public readonly bool IsPrimaryDecisionMaker;

	internal IndividualPartnerInformation(
		int customerId,
		string firstName,
		string preferredName,
		string middleName,
		string lastName,
		string suffix,
		string email,
		string culture,
		bool isPrimaryDecisionMaker)
	{
		this.customerId = customerId;
		this.FirstName = firstName;
		this.PreferredName = preferredName;
		this.MiddleName = middleName;
		this.LastName = lastName;
		this.Suffix = suffix;
		this.Email = email;
		this.Culture = culture;
		this.IsPrimaryDecisionMaker = isPrimaryDecisionMaker;
	}

	public int CustomerId
	{
		get { return this.customerId; }
	}

	internal static IndividualPartnerInformation Empty
	{
		get
		{
			return new IndividualPartnerInformation(0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false);
		}
	}
}

public sealed class AddressInformation : IHasCustomerId
{
	private readonly int customerId;

	public readonly int AddressTypeId;

	public readonly string AddressLine1;

	public readonly string AddressLine2;

	public readonly string City;

	public readonly string RegionId;

	public readonly string PostalCode;

	public readonly int CountyId;

	public readonly string County;

	public readonly string CountryId;

	public readonly double Latitude;

	public readonly double Longitude;

	internal AddressInformation(int pioneerId, int addressTypeId, string street1, string street2, string city, string postalCode, string regionId, int countyId, string county, string countryId, double latitude, double longitude)
	{
		this.customerId = pioneerId;
		this.AddressTypeId = addressTypeId;
		this.AddressLine1 = street1;
		this.AddressLine2 = street2;
		this.City = city;
		this.PostalCode = postalCode;
		this.RegionId = regionId;
		this.CountyId = countyId;
		this.County = county;
		this.CountryId = countryId;
		this.Latitude = latitude;
		this.Longitude = longitude;
	}

	public int CustomerId
	{
		get
		{
			return this.customerId;
		}
	}
}

public sealed class PhoneNumber : IHasCustomerId
{
	private readonly int customerId;

	public readonly int PhoneNumberId;

	public readonly string Number;

	public readonly int PhoneNumberTypeId;

	public PhoneNumber(int customerId, int phoneNumberId, string number, int phoneNumberTypeId)
	{
		this.customerId = customerId;
		this.PhoneNumberId = phoneNumberId;
		this.Number = number;
		this.PhoneNumberTypeId = phoneNumberTypeId;
	}

	internal PhoneNumber(PhoneNumberInformation phoneNumberInformation)
	{
		this.customerId = phoneNumberInformation.CustomerId;
		this.PhoneNumberId = phoneNumberInformation.PhoneNumberId;
		this.Number = phoneNumberInformation.Number;
		this.PhoneNumberTypeId = phoneNumberInformation.PhoneNumberTypeId;
	}

	public int CustomerId
	{
		get { return this.customerId; }
	}
}

public sealed class PhoneNumberInformation : IHasCustomerId
{
	public readonly int PhoneNumberId;

	private readonly int customerId;

	public readonly string Number;

	public readonly int PhoneNumberTypeId;

	public readonly bool IsPrimary;

	internal PhoneNumberInformation(int phoneNumberId, int pioneerId, string phoneNumber, int phoneNumberTypeId, bool isPrimary)
	{
		this.PhoneNumberId = phoneNumberId;
		this.customerId = pioneerId;
		this.Number = phoneNumber;
		this.PhoneNumberTypeId = phoneNumberTypeId;
		this.IsPrimary = isPrimary;
	}

	public int CustomerId
	{
		get { return this.customerId; }
	}
}

public sealed class IndividualPartner : IHasCustomerId
{
	public int CustomerId
	{
		get { return this.customerId; }
	}

	private readonly int customerId;

	public readonly string LastName;

	public readonly string FirstName;

	public readonly string MiddleName;

	public readonly string PreferredName;

	public readonly string Suffix;

	public readonly string Email;

	public readonly ImmutableArray<AddressInformation> Addresses;

	public readonly ImmutableArray<PhoneNumber> PhoneNumbers;

	public readonly string CultureName;

	public readonly int PrimaryPhoneNumberId;

	public IndividualPartner(int customerId, string lastName, string firstName, string middleName, string preferredName, string suffix, string email, ImmutableArray<AddressInformation> addresses, ImmutableArray<PhoneNumber> phoneNumbers, string cultureName, int primaryPhoneNumberId)
	{
		this.customerId = customerId;
		this.LastName = lastName;
		this.FirstName = firstName;
		this.MiddleName = middleName;
		this.PreferredName = preferredName;
		this.Suffix = suffix;
		this.Email = email;
		this.Addresses = addresses;
		this.PhoneNumbers = phoneNumbers;
		this.CultureName = cultureName;
		this.PrimaryPhoneNumberId = primaryPhoneNumberId;
	}

	public IndividualPartner(IndividualPartnerInformation partnerInfo, ImmutableArray<AddressInformation> addresses, ImmutableArray<PhoneNumber> phoneNumbers, int primaryPhoneNumberId)
		: this(partnerInfo.CustomerId, partnerInfo.LastName, partnerInfo.FirstName, partnerInfo.MiddleName, partnerInfo.PreferredName, partnerInfo.Suffix, partnerInfo.Email, addresses, phoneNumbers, partnerInfo.Culture, primaryPhoneNumberId)
	{
	}

	public IndividualPartner(IndividualPartner partnerInfo, ImmutableArray<AddressInformation> addresses, ImmutableArray<PhoneNumber> phoneNumbers, int primaryPhoneNumberId)
		: this(partnerInfo.CustomerId, partnerInfo.LastName, partnerInfo.FirstName, partnerInfo.MiddleName, partnerInfo.PreferredName, partnerInfo.Suffix, partnerInfo.Email, addresses, phoneNumbers, partnerInfo.CultureName, primaryPhoneNumberId)
	{
	}
}
