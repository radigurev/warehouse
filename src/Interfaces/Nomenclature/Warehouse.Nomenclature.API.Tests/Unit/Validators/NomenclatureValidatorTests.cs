using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;
using Warehouse.Nomenclature.API.Validators;
using Warehouse.ServiceModel.Requests.Nomenclature;

namespace Warehouse.Nomenclature.API.Tests.Unit.Validators;

/// <summary>
/// Tests all FluentValidation validators in the Nomenclature domain per SDD-NOM-001.
/// <para>Covers <see cref="CreateCountryRequestValidator"/>, <see cref="UpdateCountryRequestValidator"/>,
/// <see cref="CreateStateProvinceRequestValidator"/>, <see cref="UpdateStateProvinceRequestValidator"/>,
/// <see cref="CreateCityRequestValidator"/>, <see cref="UpdateCityRequestValidator"/>,
/// <see cref="CreateCurrencyRequestValidator"/>, and <see cref="UpdateCurrencyRequestValidator"/>.</para>
/// </summary>
[TestFixture]
[Category("SDD-NOM-001")]
public sealed class NomenclatureValidatorTests
{
    #region CreateCountryRequestValidator

    [TestCase("BG", true)]
    [TestCase("US", true)]
    [TestCase("bg", false)]
    [TestCase("B", false)]
    [TestCase("BGG", false)]
    [TestCase("", false)]
    public void CreateCountryRequestValidator_Iso2Code_MustBe2UppercaseLetters(string iso2Code, bool expectedValid)
    {
        // Arrange
        CreateCountryRequestValidator validator = new();
        CreateCountryRequest request = new()
        {
            Iso2Code = iso2Code,
            Iso3Code = "BGR",
            Name = "Bulgaria"
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.Errors.Where(e => e.PropertyName == "Iso2Code").Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "Iso2Code");
        }
    }

    [TestCase("BGR", true)]
    [TestCase("USA", true)]
    [TestCase("bgr", false)]
    [TestCase("BG", false)]
    [TestCase("BGRR", false)]
    [TestCase("", false)]
    public void CreateCountryRequestValidator_Iso3Code_MustBe3UppercaseLetters(string iso3Code, bool expectedValid)
    {
        // Arrange
        CreateCountryRequestValidator validator = new();
        CreateCountryRequest request = new()
        {
            Iso2Code = "BG",
            Iso3Code = iso3Code,
            Name = "Bulgaria"
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.Errors.Where(e => e.PropertyName == "Iso3Code").Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "Iso3Code");
        }
    }

    [TestCase("Bulgaria", true)]
    [TestCase("A", true)]
    [TestCase("", false)]
    public void CreateCountryRequestValidator_Name_Required(string name, bool expectedValid)
    {
        // Arrange
        CreateCountryRequestValidator validator = new();
        CreateCountryRequest request = new()
        {
            Iso2Code = "BG",
            Iso3Code = "BGR",
            Name = name
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.Errors.Where(e => e.PropertyName == "Name").Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }
    }

    [Test]
    public void CreateCountryRequestValidator_Name_ExceedsMaxLength_Fails()
    {
        // Arrange
        CreateCountryRequestValidator validator = new();
        CreateCountryRequest request = new()
        {
            Iso2Code = "BG",
            Iso3Code = "BGR",
            Name = new string('A', 101)
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorCode == "INVALID_NAME");
    }

    [Test]
    public void CreateCountryRequestValidator_PhonePrefix_Null_IsValid()
    {
        // Arrange
        CreateCountryRequestValidator validator = new();
        CreateCountryRequest request = new()
        {
            Iso2Code = "BG",
            Iso3Code = "BGR",
            Name = "Bulgaria",
            PhonePrefix = null
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void CreateCountryRequestValidator_PhonePrefix_ValidValue_IsValid()
    {
        // Arrange
        CreateCountryRequestValidator validator = new();
        CreateCountryRequest request = new()
        {
            Iso2Code = "BG",
            Iso3Code = "BGR",
            Name = "Bulgaria",
            PhonePrefix = "+359"
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void CreateCountryRequestValidator_PhonePrefix_ExceedsMaxLength_Fails()
    {
        // Arrange
        CreateCountryRequestValidator validator = new();
        CreateCountryRequest request = new()
        {
            Iso2Code = "BG",
            Iso3Code = "BGR",
            Name = "Bulgaria",
            PhonePrefix = new string('+', 11)
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhonePrefix" && e.ErrorCode == "INVALID_PHONE_PREFIX");
    }

    [Test]
    public void CreateCountryRequestValidator_AllFieldsValid_Passes()
    {
        // Arrange
        CreateCountryRequestValidator validator = new();
        CreateCountryRequest request = new()
        {
            Iso2Code = "BG",
            Iso3Code = "BGR",
            Name = "Bulgaria",
            PhonePrefix = "+359"
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region UpdateCountryRequestValidator

    [TestCase("Bulgaria", true)]
    [TestCase("", false)]
    public void UpdateCountryRequestValidator_Name_Required(string name, bool expectedValid)
    {
        // Arrange
        UpdateCountryRequestValidator validator = new();
        UpdateCountryRequest request = new()
        {
            Name = name
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.Errors.Where(e => e.PropertyName == "Name").Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }
    }

    [Test]
    public void UpdateCountryRequestValidator_Name_ExceedsMaxLength_Fails()
    {
        // Arrange
        UpdateCountryRequestValidator validator = new();
        UpdateCountryRequest request = new()
        {
            Name = new string('A', 101)
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorCode == "INVALID_NAME");
    }

    [Test]
    public void UpdateCountryRequestValidator_PhonePrefix_ExceedsMaxLength_Fails()
    {
        // Arrange
        UpdateCountryRequestValidator validator = new();
        UpdateCountryRequest request = new()
        {
            Name = "Bulgaria",
            PhonePrefix = new string('+', 11)
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhonePrefix" && e.ErrorCode == "INVALID_PHONE_PREFIX");
    }

    [Test]
    public void UpdateCountryRequestValidator_AllFieldsValid_Passes()
    {
        // Arrange
        UpdateCountryRequestValidator validator = new();
        UpdateCountryRequest request = new()
        {
            Name = "Bulgaria",
            PhonePrefix = "+359"
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region CreateStateProvinceRequestValidator

    [TestCase(0, false)]
    [TestCase(-1, false)]
    [TestCase(1, true)]
    [TestCase(999, true)]
    public void CreateStateProvinceRequestValidator_CountryId_MustBePositive(int countryId, bool expectedValid)
    {
        // Arrange
        CreateStateProvinceRequestValidator validator = new();
        CreateStateProvinceRequest request = new()
        {
            CountryId = countryId,
            Code = "SOF",
            Name = "Sofia Province"
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.Errors.Where(e => e.PropertyName == "CountryId").Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "CountryId");
        }
    }

    [TestCase("SOF", true)]
    [TestCase("AB", true)]
    [TestCase("", false)]
    public void CreateStateProvinceRequestValidator_Code_Required(string code, bool expectedValid)
    {
        // Arrange
        CreateStateProvinceRequestValidator validator = new();
        CreateStateProvinceRequest request = new()
        {
            CountryId = 1,
            Code = code,
            Name = "Sofia Province"
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.Errors.Where(e => e.PropertyName == "Code").Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "Code");
        }
    }

    [Test]
    public void CreateStateProvinceRequestValidator_Code_ExceedsMaxLength_Fails()
    {
        // Arrange
        CreateStateProvinceRequestValidator validator = new();
        CreateStateProvinceRequest request = new()
        {
            CountryId = 1,
            Code = new string('A', 11),
            Name = "Sofia Province"
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == "Code" && e.ErrorCode == "INVALID_CODE");
    }

    [TestCase("Sofia Province", true)]
    [TestCase("", false)]
    public void CreateStateProvinceRequestValidator_Name_Required(string name, bool expectedValid)
    {
        // Arrange
        CreateStateProvinceRequestValidator validator = new();
        CreateStateProvinceRequest request = new()
        {
            CountryId = 1,
            Code = "SOF",
            Name = name
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.Errors.Where(e => e.PropertyName == "Name").Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }
    }

    [Test]
    public void CreateStateProvinceRequestValidator_Name_ExceedsMaxLength_Fails()
    {
        // Arrange
        CreateStateProvinceRequestValidator validator = new();
        CreateStateProvinceRequest request = new()
        {
            CountryId = 1,
            Code = "SOF",
            Name = new string('A', 101)
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorCode == "INVALID_NAME");
    }

    [Test]
    public void CreateStateProvinceRequestValidator_AllFieldsValid_Passes()
    {
        // Arrange
        CreateStateProvinceRequestValidator validator = new();
        CreateStateProvinceRequest request = new()
        {
            CountryId = 1,
            Code = "SOF",
            Name = "Sofia Province"
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region UpdateStateProvinceRequestValidator

    [TestCase("Sofia Province", true)]
    [TestCase("", false)]
    public void UpdateStateProvinceRequestValidator_Name_Required(string name, bool expectedValid)
    {
        // Arrange
        UpdateStateProvinceRequestValidator validator = new();
        UpdateStateProvinceRequest request = new()
        {
            Name = name
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.IsValid.Should().BeTrue();
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }
    }

    [Test]
    public void UpdateStateProvinceRequestValidator_Name_ExceedsMaxLength_Fails()
    {
        // Arrange
        UpdateStateProvinceRequestValidator validator = new();
        UpdateStateProvinceRequest request = new()
        {
            Name = new string('A', 101)
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorCode == "INVALID_NAME");
    }

    #endregion

    #region CreateCityRequestValidator

    [TestCase(0, false)]
    [TestCase(-1, false)]
    [TestCase(1, true)]
    [TestCase(42, true)]
    public void CreateCityRequestValidator_StateProvinceId_MustBePositive(int stateProvinceId, bool expectedValid)
    {
        // Arrange
        CreateCityRequestValidator validator = new();
        CreateCityRequest request = new()
        {
            StateProvinceId = stateProvinceId,
            Name = "Sofia"
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.Errors.Where(e => e.PropertyName == "StateProvinceId").Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "StateProvinceId");
        }
    }

    [TestCase("Sofia", true)]
    [TestCase("", false)]
    public void CreateCityRequestValidator_Name_Required(string name, bool expectedValid)
    {
        // Arrange
        CreateCityRequestValidator validator = new();
        CreateCityRequest request = new()
        {
            StateProvinceId = 1,
            Name = name
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.Errors.Where(e => e.PropertyName == "Name").Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }
    }

    [Test]
    public void CreateCityRequestValidator_Name_ExceedsMaxLength_Fails()
    {
        // Arrange
        CreateCityRequestValidator validator = new();
        CreateCityRequest request = new()
        {
            StateProvinceId = 1,
            Name = new string('A', 101)
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorCode == "INVALID_NAME");
    }

    [Test]
    public void CreateCityRequestValidator_PostalCode_Null_IsValid()
    {
        // Arrange
        CreateCityRequestValidator validator = new();
        CreateCityRequest request = new()
        {
            StateProvinceId = 1,
            Name = "Sofia",
            PostalCode = null
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void CreateCityRequestValidator_PostalCode_ValidValue_IsValid()
    {
        // Arrange
        CreateCityRequestValidator validator = new();
        CreateCityRequest request = new()
        {
            StateProvinceId = 1,
            Name = "Sofia",
            PostalCode = "1000"
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void CreateCityRequestValidator_PostalCode_ExceedsMaxLength_Fails()
    {
        // Arrange
        CreateCityRequestValidator validator = new();
        CreateCityRequest request = new()
        {
            StateProvinceId = 1,
            Name = "Sofia",
            PostalCode = new string('1', 21)
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PostalCode" && e.ErrorCode == "INVALID_POSTAL_CODE");
    }

    [Test]
    public void CreateCityRequestValidator_AllFieldsValid_Passes()
    {
        // Arrange
        CreateCityRequestValidator validator = new();
        CreateCityRequest request = new()
        {
            StateProvinceId = 1,
            Name = "Sofia",
            PostalCode = "1000"
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region UpdateCityRequestValidator

    [TestCase("Sofia", true)]
    [TestCase("", false)]
    public void UpdateCityRequestValidator_Name_Required(string name, bool expectedValid)
    {
        // Arrange
        UpdateCityRequestValidator validator = new();
        UpdateCityRequest request = new()
        {
            Name = name
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.Errors.Where(e => e.PropertyName == "Name").Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }
    }

    [Test]
    public void UpdateCityRequestValidator_Name_ExceedsMaxLength_Fails()
    {
        // Arrange
        UpdateCityRequestValidator validator = new();
        UpdateCityRequest request = new()
        {
            Name = new string('A', 101)
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorCode == "INVALID_NAME");
    }

    [Test]
    public void UpdateCityRequestValidator_PostalCode_ExceedsMaxLength_Fails()
    {
        // Arrange
        UpdateCityRequestValidator validator = new();
        UpdateCityRequest request = new()
        {
            Name = "Sofia",
            PostalCode = new string('1', 21)
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PostalCode" && e.ErrorCode == "INVALID_POSTAL_CODE");
    }

    #endregion

    #region CreateCurrencyRequestValidator

    [TestCase("BGN", true)]
    [TestCase("USD", true)]
    [TestCase("bg", false)]
    [TestCase("BGNN", false)]
    [TestCase("BG", false)]
    [TestCase("", false)]
    public void CreateCurrencyRequestValidator_Code_MustBe3UppercaseLetters(string code, bool expectedValid)
    {
        // Arrange
        CreateCurrencyRequestValidator validator = new();
        CreateCurrencyRequest request = new()
        {
            Code = code,
            Name = "Bulgarian Lev"
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.Errors.Where(e => e.PropertyName == "Code").Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "Code");
        }
    }

    [TestCase("Bulgarian Lev", true)]
    [TestCase("", false)]
    public void CreateCurrencyRequestValidator_Name_Required(string name, bool expectedValid)
    {
        // Arrange
        CreateCurrencyRequestValidator validator = new();
        CreateCurrencyRequest request = new()
        {
            Code = "BGN",
            Name = name
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.Errors.Where(e => e.PropertyName == "Name").Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }
    }

    [Test]
    public void CreateCurrencyRequestValidator_Name_ExceedsMaxLength_Fails()
    {
        // Arrange
        CreateCurrencyRequestValidator validator = new();
        CreateCurrencyRequest request = new()
        {
            Code = "BGN",
            Name = new string('A', 101)
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorCode == "INVALID_NAME");
    }

    [Test]
    public void CreateCurrencyRequestValidator_Symbol_Null_IsValid()
    {
        // Arrange
        CreateCurrencyRequestValidator validator = new();
        CreateCurrencyRequest request = new()
        {
            Code = "BGN",
            Name = "Bulgarian Lev",
            Symbol = null
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void CreateCurrencyRequestValidator_Symbol_ValidValue_IsValid()
    {
        // Arrange
        CreateCurrencyRequestValidator validator = new();
        CreateCurrencyRequest request = new()
        {
            Code = "BGN",
            Name = "Bulgarian Lev",
            Symbol = "$"
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void CreateCurrencyRequestValidator_Symbol_ExceedsMaxLength_Fails()
    {
        // Arrange
        CreateCurrencyRequestValidator validator = new();
        CreateCurrencyRequest request = new()
        {
            Code = "BGN",
            Name = "Bulgarian Lev",
            Symbol = new string('$', 6)
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Symbol" && e.ErrorCode == "INVALID_SYMBOL");
    }

    [Test]
    public void CreateCurrencyRequestValidator_AllFieldsValid_Passes()
    {
        // Arrange
        CreateCurrencyRequestValidator validator = new();
        CreateCurrencyRequest request = new()
        {
            Code = "BGN",
            Name = "Bulgarian Lev",
            Symbol = "лв."
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region UpdateCurrencyRequestValidator

    [TestCase("Bulgarian Lev", true)]
    [TestCase("", false)]
    public void UpdateCurrencyRequestValidator_Name_Required(string name, bool expectedValid)
    {
        // Arrange
        UpdateCurrencyRequestValidator validator = new();
        UpdateCurrencyRequest request = new()
        {
            Name = name
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        if (expectedValid)
        {
            result.Errors.Where(e => e.PropertyName == "Name").Should().BeEmpty();
        }
        else
        {
            result.Errors.Should().Contain(e => e.PropertyName == "Name");
        }
    }

    [Test]
    public void UpdateCurrencyRequestValidator_Name_ExceedsMaxLength_Fails()
    {
        // Arrange
        UpdateCurrencyRequestValidator validator = new();
        UpdateCurrencyRequest request = new()
        {
            Name = new string('A', 101)
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorCode == "INVALID_NAME");
    }

    [Test]
    public void UpdateCurrencyRequestValidator_Symbol_ExceedsMaxLength_Fails()
    {
        // Arrange
        UpdateCurrencyRequestValidator validator = new();
        UpdateCurrencyRequest request = new()
        {
            Name = "Bulgarian Lev",
            Symbol = new string('$', 6)
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Symbol" && e.ErrorCode == "INVALID_SYMBOL");
    }

    [Test]
    public void UpdateCurrencyRequestValidator_AllFieldsValid_Passes()
    {
        // Arrange
        UpdateCurrencyRequestValidator validator = new();
        UpdateCurrencyRequest request = new()
        {
            Name = "Bulgarian Lev",
            Symbol = "лв."
        };

        // Act
        ValidationResult result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
