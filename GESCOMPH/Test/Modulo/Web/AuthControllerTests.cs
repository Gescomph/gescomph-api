using Business.Interfaces;
using Business.Interfaces.Implements.SecurityAuthentication;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebGESCOMPH.Controllers.Module.SecurityAuthentication;
using WebGESCOMPH.Infrastructure;

namespace Test.Modulo.Web;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _auth = new();
    private readonly Mock<IToken> _token = new();
    private readonly Mock<IAuthCookieFactory> _cookies = new();
    private readonly Mock<IOptions<JwtSettings>> _jwt = new();
    private readonly Mock<IOptions<CookieSettings>> _cookieOpts = new();
    private readonly Mock<ILogger<AuthController>> _logger = new();

    private AuthController Create()
    {
        _jwt.Setup(x => x.Value).Returns(new JwtSettings
        {
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        });

        _cookieOpts.Setup(x => x.Value).Returns(new CookieSettings
        {
            AccessTokenName = "at",
            RefreshTokenName = "rt",
            CsrfCookieName = "csrf"
        });

        _cookies.Setup(c => c.AccessCookieOptions(It.IsAny<DateTimeOffset>()))
            .Returns(new CookieOptions());
        _cookies.Setup(c => c.RefreshCookieOptions(It.IsAny<DateTimeOffset>()))
            .Returns(new CookieOptions());
        _cookies.Setup(c => c.CsrfCookieOptions(It.IsAny<DateTimeOffset>()))
            .Returns(new CookieOptions());

        return new AuthController(
            _auth.Object,
            _token.Object,
            _cookies.Object,
            _jwt.Object,
            _cookieOpts.Object,
            _logger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    // ======================================================
    // 1. Refresh sin refresh token -> 401
    // ======================================================
    [Fact]
    public async Task RefreshUnauthorizedWhenNoRefreshCookie()
    {
        var ctrl = Create();

        var res = await ctrl.Refresh();

        Assert.IsType<UnauthorizedObjectResult>(res);
    }

    // ======================================================
    // 2. CSRF ausente -> 401
    // ======================================================
    [Fact]
    public async Task RefreshUnauthorizedWhenCsrfHeaderMissing()
    {
        var ctrl = Create();

        ctrl.ControllerContext.HttpContext.Request.Headers["Cookie"] =
            "rt=value; csrf=abc";

        var res = await ctrl.Refresh();

        Assert.IsType<UnauthorizedObjectResult>(res);
    }

    // ======================================================
    // 3. Login correcto coloca cookies
    // ======================================================
    [Fact]
    public async Task LoginOkSetsCookies()
    {
        var tokens = new TokenResponseDto
        {
            AccessToken = "acc",
            RefreshToken = "ref",
            CsrfToken = "csrf"
        };

        var loginResult = new LoginResultDto
        {
            RequiresTwoFactor = false,
            Tokens = tokens
        };

        _auth.Setup(a => a.LoginAsync(It.IsAny<LoginDto>()))
             .ReturnsAsync(loginResult);

        var ctrl = Create();

        var res = await ctrl.Login(new LoginDto { Email = "a@mail", Password = "x" });
        var ok = Assert.IsType<OkObjectResult>(res);

        var setCookieHeader = ctrl.ControllerContext.HttpContext.Response.Headers["Set-Cookie"].ToString();

        Assert.Contains("at=acc", setCookieHeader);
        Assert.Contains("rt=ref", setCookieHeader);
        Assert.Contains("csrf=csrf", setCookieHeader);

        var dict = ok.Value!
            .GetType()
            .GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(ok.Value)!);

        Assert.True((bool)dict["isSuccess"]);
    }

    // ======================================================
    // 4. Refresh correcto: rota cookies y devuelve OK
    // ======================================================
    [Fact]
    public async Task RefreshOkReturnsNewTokensAndSetsCookies()
    {
        var refreshResponse = new TokenRefreshResponseDto
        {
            AccessToken = "newAcc",
            RefreshToken = "newRef",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        _token.Setup(t => t.RefreshAsync(It.IsAny<TokenRefreshRequestDto>()))
              .ReturnsAsync(refreshResponse);

        var ctrl = Create();

        ctrl.ControllerContext.HttpContext.Request.Headers["Cookie"] =
            "rt=oldRefresh; csrf=abc";

        ctrl.ControllerContext.HttpContext.Request.Headers["X-XSRF-TOKEN"] = "abc";

        var res = await ctrl.Refresh();
        var ok = Assert.IsType<OkObjectResult>(res);

        var setCookieHeader = ctrl.ControllerContext.HttpContext.Response.Headers["Set-Cookie"].ToString();

        Assert.Contains("at=newAcc", setCookieHeader);
        Assert.Contains("rt=newRef", setCookieHeader);

        var dict = ok.Value!
            .GetType()
            .GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(ok.Value)!);

        Assert.True((bool)dict["isSuccess"]);
    }

}
