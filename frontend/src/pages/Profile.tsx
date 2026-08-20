import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMe, useUpdateMe } from '@/api/customers';
import { Card, CardBody, CardHeader } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Spinner';
import { useToast } from '@/components/ui/Toast';
import { ApiError } from '@/api/client';
import { formatDate } from '@/lib/format';

const schema = z.object({
  firstName: z.string().min(1, 'Required'),
  lastName: z.string().min(1, 'Required'),
  phoneNumber: z.string().min(7, 'Enter a valid phone number'),
  street: z.string().min(1, 'Required'),
  city: z.string().min(1, 'Required'),
  state: z.string().min(1, 'Required'),
  zipCode: z.string().min(1, 'Required'),
  country: z.string().min(1, 'Required'),
});
type FormValues = z.infer<typeof schema>;

export function Profile() {
  const { data: customer, isLoading } = useMe(true);
  const updateMe = useUpdateMe();
  const toast = useToast();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  useEffect(() => {
    if (customer) {
      reset({
        firstName: customer.firstName,
        lastName: customer.lastName,
        phoneNumber: customer.phoneNumber,
        street: customer.address.street,
        city: customer.address.city,
        state: customer.address.state,
        zipCode: customer.address.zipCode,
        country: customer.address.country,
      });
    }
  }, [customer, reset]);

  async function onSubmit(values: FormValues) {
    try {
      await updateMe.mutateAsync({
        firstName: values.firstName,
        lastName: values.lastName,
        phoneNumber: values.phoneNumber,
        address: {
          street: values.street,
          city: values.city,
          state: values.state,
          zipCode: values.zipCode,
          country: values.country,
        },
      });
      toast.success('Profile updated.');
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Could not update profile.');
    }
  }

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner />
      </div>
    );
  }

  return (
    <div className="flex max-w-lg flex-col gap-6">
      <div>
        <h1 className="text-xl font-semibold text-(--color-text)">Profile</h1>
        <p className="mt-0.5 text-sm text-(--color-text-muted)">
          {customer?.email} · customer since {customer && formatDate(customer.createdAt)}
        </p>
      </div>

      <Card>
        <CardHeader title="Personal details" />
        <CardBody>
          <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
            <div className="grid grid-cols-2 gap-4">
              <Input label="First name" {...register('firstName')} error={errors.firstName?.message} />
              <Input label="Last name" {...register('lastName')} error={errors.lastName?.message} />
            </div>
            <Input label="Phone number" {...register('phoneNumber')} error={errors.phoneNumber?.message} />

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

            <Button type="submit" loading={updateMe.isPending} disabled={!isDirty} className="w-fit">
              Save changes
            </Button>
          </form>
        </CardBody>
      </Card>
    </div>
  );
}
