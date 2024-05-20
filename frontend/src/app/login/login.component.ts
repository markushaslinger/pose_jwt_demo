import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { MatCard, MatCardActions, MatCardContent, MatCardHeader, MatCardTitle } from "@angular/material/card";
import { MatError, MatFormField } from "@angular/material/form-field";
import { MatButton } from "@angular/material/button";
import { MatInput } from "@angular/material/input";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { ILoginRequest } from "../../core/model";

@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatCard,
    MatCardHeader,
    MatCardContent,
    MatCardTitle,
    MatError,
    MatFormField,
    MatCardActions,
    MatButton,
    MatInput,
    ReactiveFormsModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);

  public loginForm = this.fb.group({
    username: ['', Validators.required],
    password: ['', Validators.required]
  });

  public async submit(): Promise<void> {
    const data: ILoginRequest = this.loginForm.value as ILoginRequest;
    console.log(JSON.stringify(data));
  }
}
