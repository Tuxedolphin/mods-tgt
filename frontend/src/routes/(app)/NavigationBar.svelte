<script lang="ts">
  import { Menu } from "@lucide/svelte";

  import mods_tgt_header from "$lib/assets/mods_tgt_header.png?enhanced";
  import type { LayoutProps } from "../$types";
  import UserAvatar from "./UserAvatar.svelte";

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  let { children }: LayoutProps = $props();

  import { currentUserInformation } from "$lib/shared/shared.svelte";
</script>

<div class="navbar bg-base-200 shadow-sm">
  <div class="container flex justify-between mx-auto">
    <div class="flex items-center">
      <label for="main-drawer" aria-label="open sidebar">
        <Menu></Menu>
      </label>
      <enhanced:img
        class="aspect-5/2 h-14 w-auto align-middle"
        src={mods_tgt_header}
        alt="Mods Together Logo"
      />
    </div>
    <div class="flex items-center gap-1">
      <p>@{$currentUserInformation.handle}</p>
      <UserAvatar></UserAvatar>
    </div>
  </div>
</div>

<div class="container mx-auto">
  {@render children()}
</div>
