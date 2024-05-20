import { ChangeDetectionStrategy, Component, computed, inject, Signal } from "@angular/core";
import { RouterLink, RouterOutlet } from "@angular/router";
import { MatToolbar } from "@angular/material/toolbar";
import { MatButton, MatIconButton } from "@angular/material/button";
import { AsyncPipe } from "@angular/common";
import { MatIcon } from "@angular/material/icon";
import { MatListItem, MatNavList } from "@angular/material/list";
import { MatSidenav, MatSidenavContainer, MatSidenavContent } from "@angular/material/sidenav";
import { BreakpointObserver, Breakpoints } from "@angular/cdk/layout";
import { map, shareReplay } from "rxjs/operators";
import { toSignal } from "@angular/core/rxjs-interop";

@Component({
  selector: "app-root",
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, MatToolbar, MatButton, AsyncPipe, MatIcon, MatIconButton, MatListItem, MatNavList, MatSidenav, MatSidenavContainer, MatSidenavContent, RouterLink, RouterOutlet],
  templateUrl: "./app.component.html",
  styleUrl: "./app.component.scss"
})
export class AppComponent {
  private readonly breakpointObserver = inject(BreakpointObserver);

  public readonly title: string = "JWT Demo";
  public showTitleInMainToolbar: Signal<boolean> = computed(() => this.isHandset());
  public showTitleInSidenav: Signal<boolean> = computed(() => !this.showTitleInMainToolbar());
  public isHandset: Signal<boolean> = toSignal(this.breakpointObserver
    .observe(Breakpoints.Handset)
    .pipe(
      map(result => result.matches),
      shareReplay()
    ), { initialValue: false });

}
