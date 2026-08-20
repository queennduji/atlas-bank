import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Link } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import { CheckCircle2, Landmark } from 'lucide-react';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { Card, CardBody, CardHeader } from '@/components/ui/Card';
import { useRegisterCustomer } from '@/api/customers';
import { ApiError } from '@/api/client';

const schema = z.object({
  firstName: z.string().min(1, 'Required'),
  lastName: z.string().min(1, 'Required'),
  email: z.string().email('Enter a valid email'),
  password: z.string().min(8, 'At least 8 characters'),
  phoneNumber: z.string().min(7, 'Enter a valid phone number'),
  dateOfBirth: z.string().min(1, 'Required'),
  street: z.string().min(1, 'Required'),
  city: z.string().min(1, 'Required'),
  state: z.string().min(1, 'Required'),
  zipCode: z.string().min(1, 'Required'),
  country: z.string().min(1, 'Required'),
});

type FormValues = z.infer<typeof schema>;

export function Register() {
  const auth = useAuth();
  const registerCustomer = useRegisterCustomer();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [done, setDone] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  async function onSubmit(values: FormValues) {
    setSubmitError(null);
    try {
      await registerCustomer.mutateAsync({
        firstName: values.firstName,
        lastName: values.lastName,
        email: values.email,
        password: values.password,
        phoneNumber: values.phoneNumber,
        dateOfBirth: values.dateOfBirth,
        address: {
          street: values.street,
          city: values.city,
          state: values.state,
          zipCode: values.zipCode,
          country: values.country,
        },
      });
      setDone(true);
    } catch (err) {
      setSubmitError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.');
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-(--color-bg) px-4 py-10">
      <div className="w-full max-w-lg">
        <Link to="/" className="mb-6 flex items-center justify-center gap-2">
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-(--color-brand) text-white">
            <Landmark size={16} />
          </div>
          <span className="text-sm font-semibold tracking-tight text-(--color-text)">Atlas Bank</span>
        </Link>

        <Card>
          {done ? (
            <CardBody className="flex flex-col items-center gap-3 py-10 text-center">
              <CheckCircle2 size={36} className="text-(--color-positive)" />
              <h2 className="text-lg font-semibold text-(--color-text)">Account created</h2>
              <p className="max-w-xs text-sm text-(--color-text-muted)">
                Your customer profile is set up. Sign in to open your first account.
              </p>
              <Button className="mt-2" onClick={() => auth.signinRedirect()}>
                Sign in
              </Button>
            </CardBody>
          ) : (
            <>
              <CardHeader title="Open an account" subtitle="Takes about a minute." />
              <CardBody>
                <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
                  <div className="grid grid-cols-2 gap-4">
                    <Input label="First name" {...register('firstName')} error={errors.firstName?.message} />
                    <Input label="Last name" {...register('lastName')} error={errors.lastName?.message} />
                  </div>
                  <Input label="Email" type="email" {...register('email')} error={errors.email?.message} />
                  <Input
                    label="Password"
                    type="password"
                    {...register('password')}
                    error={errors.password?.message}
                  />
                  <div className="grid grid-cols-2 gap-4">
                    <Input label="Phone number" {...register('phoneNumber')} error={errors.phoneNumber?.message} />
                    <Input
                      label="Date of birth"
                      type="date"
                      {...register('dateOfBirth')}
                      error={errors.dateOfBirth?.message}
                    />
                  </div>

                  <div className="border-t border-(--color-border) pt-4">
                    <p className="mb-3 text-sm font-medium text-(--color-text)">Mailing address</p>
                    <div className="flex flex-col gap-4">
                      <Input label="Street" {...register('street')} error={errors.street?.message} />
                      <div className="grid grid-cols-2 gap-4">
                        <Input label="City" {...register('city')} error={errors.city?.message} />
                        <Input label="State" {...register('state')} error={errors.state?.message} />
                      </div>
                      <div className="grid grid-cols-2 gap-4">
                        <Input label="ZIP code" {...register('zipCode')} error={errors.zipCode?.message} />
                        <Input label="Country" {...register('country')} error={errors.country?.message} />
                      </div>
                    </div>
                  </div>

                  {submitError && <p className="text-sm text-(--color-negative)">{submitError}</p>}

                  <Button type="submit" loading={registerCustomer.isPending} className="mt-1">
                    Create account
                  </Button>
                </form>
              </CardBody>
            </>
          )}
        </Card>

        {!done && (
          <p className="mt-4 text-center text-sm text-(--color-text-muted)">
            Already have an account?{' '}
            <button onClick={() => auth.signinRedirect()} className="font-medium text-(--color-brand)">
              Sign in
            </button>
          </p>
        )}
      </div>
    </div>
  );
}
