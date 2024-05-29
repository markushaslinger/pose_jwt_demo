import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  signal,
  WritableSignal
} from "@angular/core";
import { DateTimeFormatter, LocalDateTime } from "@js-joda/core";

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit, OnDestroy {
  private static readonly timeFormatter = DateTimeFormatter.ofPattern("HH:mm:ss");
  private timeUpdateDelegateId: ReturnType<typeof setInterval> | null = null
  public currentTime: WritableSignal<string> = signal(HomeComponent.getFormattedCurrentTime());

  public ngOnInit(): void {
    this.timeUpdateDelegateId = setInterval(() => {
      const timeStr = HomeComponent.getFormattedCurrentTime();
      if (this.currentTime() !== timeStr) {
        this.currentTime.set(timeStr);
      }
    }, 500);
  }

  public ngOnDestroy(): void {
    if (this.timeUpdateDelegateId !== null) {
      clearInterval(this.timeUpdateDelegateId);
    }
  }

  private static getFormattedCurrentTime(): string {
    const now = LocalDateTime.now();
    return now.format(HomeComponent.timeFormatter);
  }

}
