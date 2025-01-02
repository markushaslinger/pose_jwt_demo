import { ChangeDetectionStrategy, Component, inject, OnDestroy, OnInit, signal, WritableSignal } from "@angular/core";
import { DateTimeFormatter, LocalDateTime } from "@js-joda/core";
import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { HUB_BASE_URL } from "../../../core/const";
import { z } from "zod";
import { AuthService } from "../../../core/auth/auth.service";
import { MatCard, MatCardContent } from "@angular/material/card";

@Component({
    selector: 'app-home-time-display',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        MatCard,
        MatCardContent
    ],
    templateUrl: './home-time-display.component.html',
    styleUrl: './home-time-display.component.scss'
})
export class HomeTimeDisplayComponent implements OnInit, OnDestroy {
  private readonly authService: AuthService = inject(AuthService);
  private hubConnection: HubConnection | null = null;
  public readonly currentTime: WritableSignal<string> = signal('not yet received');

  public async ngOnInit(): Promise<void> {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${HUB_BASE_URL}/time`, {
        accessTokenFactory: async () => {
          // we don't have to worry about refresh, the token is needed for initial connection only
          // that also means, that you have to keep in mind that active websocket connections
          // will not be closed automatically when the token expires - if that is important
          // you have to add extra logic in the hub to handle that case
          const token = await this.authService.getAccessToken();
          return token?.token ?? "";
        }
      })
      .build();

    try {
      await this.hubConnection.start();
      console.log("Connected to time hub");

      this.currentTime.set('connected and waiting');

      this.hubConnection.on("ReceiveTime", (data: unknown) => {
        const processor = new TimeUpdateProcessor(data);
        const formattedTime = processor.process();
        if (formattedTime) {
          this.currentTime.set(formattedTime);
        }
      });
      this.hubConnection.onreconnected(_ => console.log("Reconnected to time hub"));
      this.hubConnection.onclose(error => console.error("Connection to time hub closed", error));
    } catch (error) {
      console.error(error);
    }
  }

  public async ngOnDestroy(): Promise<void> {
    await this.hubConnection?.stop();
  }
}

class TimeUpdateProcessor{
  private static readonly timeFormatter = DateTimeFormatter.ofPattern("HH:mm:ss");

  constructor(private readonly rawData: unknown) {}

  public process(): string | null{
    const timeUpdate = TimeUpdateProcessor.parse(this.rawData);
    if (!timeUpdate) {
      return null;
    }

    return TimeUpdateProcessor.format(timeUpdate);
  }

  private static format(timeUpdate: ITimeUpdate): string {
    const resolutionLevel = timeUpdate.quality === TimeQuality.High
      ? "high"
      : "low";
    const resolutionInfo = `(${resolutionLevel} resolution)`;
    const formattedTime = timeUpdate.currentTime.format(TimeUpdateProcessor.timeFormatter);
    return `${formattedTime} ${resolutionInfo}`;
  }

  private static parse(data: unknown): ITimeUpdate | null{
    try {
      return TimeUpdateWire.parse(data) as ITimeUpdate;
    }
    catch (error){
      console.error("Failed to parse time update", error);
      return null;
    }
  }
}

enum TimeQuality {
  Low,
  High
}

const TimeUpdateWire = z.object({
  currentTime: z.string().transform(s => LocalDateTime.parse(s)),
  quality: z.nativeEnum(TimeQuality)
});

interface ITimeUpdate {
  currentTime: LocalDateTime;
  quality: TimeQuality;
}
