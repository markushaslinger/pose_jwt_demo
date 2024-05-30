import {
  ChangeDetectionStrategy,
  Component,
} from "@angular/core";
import { HomeTimeDisplayComponent } from "./home-time-display/home-time-display.component";

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    HomeTimeDisplayComponent
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {

}
