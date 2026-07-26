<script lang="ts">
  import type { PageProps } from "./$types";
  import zhu_zhen from "$lib/assets/zhuzhen.jpg?enhanced";
  import xin_jie from "$lib/assets/xinjie.jpg?enhanced";
  import jordan from "$lib/assets/jordan.jpg?enhanced";
  import nus_mods_logo from "$lib/assets/nusmods_logo.png?enhanced";
  import { HandHeart } from "@lucide/svelte";
  let { data }: PageProps = $props();
</script>

<p class="mt-2 text-xl font-semibold">Credits & Attributions!</p>
<hr class="my-2 h-px border-0 bg-gray-200" />

<p class="mt-2 font-semibold">About</p>
<hr class="my-2 h-px border-0 bg-gray-200" />

<div class="text-wrap wrap-break-word text-justify">
  <div class="flex flex-col gap-2">
    <p class="text-justify">
      ModsTogether is a project created by Zhuzhen and Jordan, under the team
      name Zhun, for NUS Orbital 2026. It allows you to plan mods live with
      friends, and supports live syncing of timetable data across devices.
    </p>

    <p class="text-justify">
      This project was created to solve an issue that is often seen when
      planning mods, which is during the stressful time of course registration,
      everyone is sending copies of their timetables to different groups of
      friends, and it makes it a logistical nightmare for the you, as well as a
      bad experience for your phone's storage too.
    </p>
  </div>

  <p class="mt-2 font-semibold">Core Team</p>
  <hr class="my-2 h-px border-0 bg-gray-200" />
  <div class="flex p-4">
    <div>
      <enhanced:img
        class="object-contain min-w-36 max-w-36 rounded-full"
        src={zhu_zhen}
      />
    </div>

    <div class="flex flex-col px-6">
      <div class=" text-2xl font-semibold">Zhuzhen</div>
      <div class="uppercase text-sm font-semibold opacity-60">
        Backend & Database Development
      </div>
      <div class="italic opacity-80">
        Responsible for the creation and storage of your ModsTogether data, as
        well as the live communication features!
      </div>
      <div>Hello there 🙂</div>
    </div>
  </div>

  <div class="flex p-4">
    <div>
      <enhanced:img
        class="object-contain min-w-36 max-w-36 rounded-full"
        src={jordan}
      />
    </div>

    <div class="flex flex-col px-6">
      <div class=" text-2xl font-semibold">Jordan</div>
      <div class="uppercase text-sm font-semibold opacity-60">
        Frontend Development & CI/CD
      </div>
      <div class="italic opacity-80">
        Responsible for the look and feel of ModsTogether, as well as the
        deployment of it to your devices!
      </div>
      <div>
        I love to learn languages, both real ones and ones in the digital world.
        My hobbies include game development (both playing and making), reading
        and taking photos. I enjoy making products that can help people or make
        them happy, hope ModsTogether has done that for you. 😊 You can find me
        on my <a
          href="https://jyodann.com/"
          class="link link-hover link-primary"
          target="_blank">website</a
        >
        or
        <a
          href="https://github.com/jyodann"
          class="link link-hover link-primary"
          target="_blank">GitHub!</a
        >
      </div>
    </div>
  </div>

  <p class="mt-2 font-semibold">Special Thanks!</p>
  <hr class="my-2 h-px border-0 bg-gray-200" />

  <div class="flex p-4">
    <div>
      <enhanced:img
        class="object-contain min-w-36 max-w-36 rounded-full"
        src={xin_jie}
      />
    </div>

    <div class="flex flex-col px-6">
      <div class=" text-2xl font-semibold">Xin Jie</div>
      <div class="uppercase font-semibold opacity-60 text-sm">Artist</div>
      <div class="italic">
        The mighty mind behind all the cute characters you see in ModsTogether,
        as well as the logo too!
      </div>
      <div>
        digital art is my favourite hobby and it's been a pleasure to draw all
        the cute and chubby characters around the website! you can find more of
        my art at <a
          class="link link-hover link-primary"
          href="https://www.instagram.com/footlongburger"
          target="_blank">@footlongburger</a
        > on instagram!
      </div>
    </div>
  </div>

  <div class="flex p-4">
    <div>
      <enhanced:img
        class="object-contain min-w-36 max-w-36 rounded-full"
        src={nus_mods_logo}
      />
    </div>

    <div class="flex flex-col px-6">
      <div class=" text-2xl font-semibold text-wrap break-all">
        Team @nusmodifications
      </div>
      <div class="uppercase font-semibold opacity-60 text-sm">
        Creators of NUSMods & NUSMods API
      </div>
      <div class="italic">The team that started it all</div>
      <div>
        ModsTogether uses data that is painstakingly maintained and gathered by
        the core team <a
          class="link link-hover link-primary"
          href="https://github.com/nusmodifications"
          target="_blank">@nusmodifications</a
        > and this project would not be possible without the countless hours they
        have spent to create such a good product in the first place! Be sure to show
        them lots of love!
      </div>
    </div>
  </div>

  <div class="flex p-4">
    <div
      class="bg-base-300 rounded-full min-w-36 min-h-36 max-h-36 flex items-center justify-center"
    >
      <HandHeart size={120} />
    </div>

    <div class="flex flex-col px-6">
      <div class=" text-2xl font-semibold">Friends, Community & You. ❤️</div>
      <div class="uppercase font-semibold opacity-60 text-sm">
        Testing and Encouraging
      </div>
      <div class="italic">The ones that made it all possible.</div>
      <div>
        Special Thanks go especially to our friends: Clarissa, Xuan Hao, Wan Xi
        & Halyn for supporting us during this journey. To the community, whether
        you helped test ModsTogether during infancy, or were with us when we
        went insane over this project, it would not have been possible without
        your support. Thank you for using ModsTogether. We ride together, we
        mods together. 😊
        <p class="text-right italic">With love, from Zhuzhen and Jordan</p>
      </div>
    </div>
  </div>
</div>
