export class LocalStorage {
  constructor(private readonly prefix: string) {
  }

  public set<T>(key: string, value: T): void {
    localStorage.setItem(this.getKey(key), JSON.stringify(value));
  }

  public get<T>(key: string): T | null {
    const item = localStorage.getItem(this.getKey(key));
    if (item) {
      return JSON.parse(item);
    }
    return null;
  }

  public remove(key: string): void {
    localStorage.removeItem(this.getKey(key));
  }

  private getKey(key: string): string {
    return `${this.prefix}.${key}`;
  }
}
