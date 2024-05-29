import { inject, Injectable } from "@angular/core";
import { HttpClient, HttpContext } from "@angular/common/http";
import { TOKEN_INTERCEPTION_DISABLED } from "./interceptor";
import { BASE_URL } from "../const";
import { lastValueFrom } from "rxjs";
import { Duration, Instant } from "@js-joda/core";
import { z } from "zod";
import { LocalStorage } from "../shared/local-storage";
import { UserService } from "../user/user.service";

@Injectable({
  providedIn: "root"
})
export class AuthService {

  private static readonly accessTokenKey = "accessToken";
  private static readonly refreshTokenKey = "refreshToken";
  private static readonly expirationThreshold = Duration.ofMinutes(2);
  private readonly baseUrl = `${BASE_URL}/auth`;
  private readonly client = inject(HttpClient);
  private readonly localStorage = new LocalStorage("Auth");

  private static getNoTokenContext(): HttpContext {
    return new HttpContext().set(TOKEN_INTERCEPTION_DISABLED, false);
  }

  public async getAccessToken(): Promise<IToken | null> {
    const accessToken = this.localStorage.get<IToken>(AuthService.accessTokenKey);
    if (accessToken !== null) {
      const expiration = new TokenExpiration(accessToken);
      if (expiration.isExpired()
        || expiration.isRemainingTimeShorterThan(AuthService.expirationThreshold)) {
        return await this.tryTokenRefresh(accessToken);
      }

      return accessToken;
    }

    return null;
  }

  public async login(username: string, password: string): Promise<IToken | null> {
    const url = `${this.baseUrl}/logins`;

    this.logout();

    const payload = {
      username,
      password
    };
    const response = await lastValueFrom(this.client.post(url, payload,
      {
        context: AuthService.getNoTokenContext(),
        observe: "response"
      }));

    if (response.ok) {
      const data = TokenResponseWire.parse(response.body);
      return this.saveReceivedTokens(data);
    }

    if (response.status === 401) {
      console.log("Username or password incorrect");
    } else {
      console.error(`Failed to login: ${response.status}`);
    }

    return null;
  }

  public logout(): void {
    this.localStorage.remove(AuthService.accessTokenKey);
    this.localStorage.remove(AuthService.refreshTokenKey);
  }

  private async tryTokenRefresh(accessToken: IToken): Promise<IToken | null> {
    const refreshToken = this.localStorage.get<IToken>(AuthService.refreshTokenKey);
    if (refreshToken === null) {
      console.log("No refresh token available");
      return null;
    }

    const expiration = new TokenExpiration(refreshToken);
    if (expiration.isExpired()) {
      console.log("Refresh token expired");
      return null;
    }

    const username = UserService.decodeToken(accessToken.token)?.username;
    if (!username) {
      console.error("Failed to decode username from access token");
      return null;
    }

    const url = `${this.baseUrl}/token-refreshes`;
    const payload = {
      username: username,
      refreshToken: refreshToken.token
    };
    const response = await lastValueFrom(this.client.post(url, payload,
      {
        context: AuthService.getNoTokenContext(),
        observe: "response"
      }));

    if (response.ok) {
      return this.saveReceivedTokens(TokenResponseWire.parse(response.body));
    }

    if (response.status === 401) {
      console.log("Refresh token invalid");
    } else {
      console.error(`Failed to refresh token: ${response.status}`);
    }

    return null;
  }

  private saveReceivedTokens(data: TokenResponse): IToken {
    const accessToken = data.accessToken as IToken;
    const refreshToken = data.refreshToken as IToken;

    this.localStorage.set(AuthService.accessTokenKey, accessToken);
    this.localStorage.set(AuthService.refreshTokenKey, refreshToken);

    return accessToken;
  }
}

export interface IToken {
  token: string;
  expiration: Instant;
}

type TokenData = z.infer<typeof TokenDataWire>;

const TokenDataWire = z.object({
  token: z.string(),
  expiration: z.string().transform(s => Instant.parse(s))
});

type TokenResponse = z.infer<typeof TokenResponseWire>;

const TokenResponseWire = z.object({
  accessToken: TokenDataWire,
  refreshToken: TokenDataWire
});

class TokenExpiration {

  private readonly now;

  constructor(private readonly token: IToken) {
    this.now = Instant.now();
  }

  public isExpired(): boolean {
    return this.now.isAfter(this.token.expiration);
  }

  public isRemainingTimeShorterThan(duration: Duration): boolean {
    return this.timeUntilExpiration().minus(duration).isNegative();
  }

  private timeUntilExpiration(): Duration {
    return Duration.between(this.now, this.token.expiration);
  }

}